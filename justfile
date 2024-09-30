# Generate SDK's from a swagger.json file.
#
#  Ensure that you set the following environment variables to an appropriate value before running
#    PACKAGE_NAME
#    PROJECT_NAME
#    ASSEMBLY_VERSION
#    PACKAGE_VERSION
#    APPLICATION_NAME
#    META_REQUEST_ID_HEADER_KEY
#    NUGET_PACKAGE_LOCATION
#    FBN_BASE_URL

#  Possible Application Names. Application name is used to retrieve the correct endpoint.
#  Underscores can be used in place of dashes
#  lusid
#  honeycomb
#  lusid-identity
#  lusid-access
#  lusid-drive
#  notifications
#  scheduler2
#  insights
#  configuration


export PACKAGE_NAME               := `echo ${PACKAGE_NAME:-Lusid.Sdk}`
export PROJECT_NAME               := `echo ${PROJECT_NAME:-Lusid.Sdk}`
export ASSEMBLY_VERSION           := `echo ${ASSEMBLY_VERSION:-2.0.0}`
export PACKAGE_VERSION            := `echo ${PACKAGE_VERSION:-2.9999.0-alpha.nupkg}`
export APPLICATION_NAME           := `echo ${APPLICATION_NAME:-lusid}`
export APPLICATION_SHORT_NAME     := `echo ${APPLICATION_SHORT_NAME:-lusid}`
export META_REQUEST_ID_HEADER_KEY := `echo ${META_REQUEST_ID_HEADER_KEY:-lusid-meta-requestid}`
export NUGET_PACKAGE_LOCATION     := `echo ${NUGET_PACKAGE_LOCATION:-~/.nuget/local-packages}`
export EXCLUDE_TESTS              := `echo ${EXCLUDE_TESTS:-true}`
export GIT_REPO_NAME              := `echo ${GIT_REPO_NAME:-}`
export TEST_API                   := `echo ${TEST_API:-ApplicationMetadataApi}`
export TEST_METHOD                := `echo ${TEST_METHOD:-'ListAccessControlledResources('}`
export ASYNC_TEST_METHOD          := `echo ${ASYNC_TEST_METHOD:-'ListAccessControlledResourcesAsync('}`

swagger_path := "./swagger.json"

swagger_url := "https://example.lusid.com/api/swagger/v0/swagger.json"

get-swagger:
    echo {{swagger_url}}
    curl -s {{swagger_url}} > swagger.json

build-docker-images: 
    docker build -t finbourne/lusid-sdk-gen-csharp:latest --ssh default=$SSH_AUTH_SOCK --platform linux/amd64/v2 -f Dockerfile .

generate-local:
    envsubst < generate/config-template.json > generate/.config.json
    rm -rf generate/.output || true
    cp generate/templates/description.{{APPLICATION_SHORT_NAME}}.mustache generate/templates/description.mustache
    docker run \
        -e JAVA_OPTS="-Dlog.level=error" \
        -e APPLICATION_NAME=${APPLICATION_NAME} \
        -e APPLICATION_SHORT_NAME=${APPLICATION_SHORT_NAME} \
        -e META_REQUEST_ID_HEADER_KEY=${META_REQUEST_ID_HEADER_KEY} \
        -e ASSEMBLY_VERSION=${ASSEMBLY_VERSION} \
        -e GIT_REPO_NAME=${GIT_REPO_NAME} \
        -e EXCLUDE_TESTS=${EXCLUDE_TESTS} \
        -e PACKAGE_VERSION=${PACKAGE_VERSION} \
        -v $(pwd)/generate/:/usr/src/generate/ \
        -v $(pwd)/generate/.openapi-generator-ignore:/usr/src/generate/.output/.openapi-generator-ignore \
        -v $(pwd)/{{swagger_path}}:/tmp/swagger.json \
        finbourne/lusid-sdk-gen-csharp:latest -- ./generate/generate.sh ./generate ./generate/.output /tmp/swagger.json .config.json
    rm -f generate/.output/.openapi-generator-ignore
    rm generate/templates/description.mustache
    
    # split the README into two, and move one up a level
    bash generate/split-readme.sh

    # clone the RestSharp fork into the solution
    # git clone git@gitlab.com:finbourne/ctools/sdk-core.git generate/.output/sdk/RestSharp
    
generate TARGET_DIR:
    @just generate-local
    
    # need to remove the created content before copying over the top of it.
    # this prevents deleted content from hanging around indefinitely.
    rm -rf {{TARGET_DIR}}/sdk
    rm -rf {{TARGET_DIR}}/docs
    cp -R generate/.output/* {{TARGET_DIR}}


# Generate an SDK from a swagger.json and copy the output to the TARGET_DIR
generate-cicd TARGET_DIR:
    mkdir -p {{TARGET_DIR}}
    mkdir -p ./generate/.output
    envsubst < generate/config-template.json > generate/.config.json
    cp ./generate/.openapi-generator-ignore ./generate/.output/.openapi-generator-ignore
    cp ./generate/templates/description.{{APPLICATION_SHORT_NAME}}.mustache ./generate/templates/description.mustache

    ./generate/generate.sh ./generate ./generate/.output {{swagger_path}} .config.json
    rm -f generate/.output/.openapi-generator-ignore
    git clone git@gitlab.com:finbourne/ctools/sdk-core.git ./generate/.output/sdk/RestSharp
    rm -rf ./generate/.output/sdk/RestSharp/.git

    # split the README into two, and move one up a level
    bash generate/split-readme.sh

    # need to remove the created content before copying over the top of it.
    # this prevents deleted content from hanging around indefinitely.
    rm -rf {{TARGET_DIR}}/sdk/${APPLICATION_NAME}
    rm -rf {{TARGET_DIR}}/sdk/docs
    rm -rf {{TARGET_DIR}}/RestSharp
    
    cp -R generate/.output/. {{TARGET_DIR}}
    echo "copied output to {{TARGET_DIR}}"
    ls {{TARGET_DIR}}

publish-only-local:
    docker run \
        -e PACKAGE_VERSION=${PACKAGE_VERSION} \
        -v $(pwd)/generate/.output:/usr/src/ \
        finbourne/lusid-sdk-gen-csharp:latest -- bash -c "cd /usr/src/sdk; dotnet pack -c Release /p:AssemblyVersion=${ASSEMBLY_VERSION} /p:PackageVersion=${PACKAGE_VERSION} /p:PackageId=${PACKAGE_NAME} "
    mkdir -p ${NUGET_PACKAGE_LOCATION}
    find . -name "*.nupkg" -exec cp {} ${NUGET_PACKAGE_LOCATION} \;

publish-only:
    docker run \
        -e PACKAGE_VERSION=${PACKAGE_VERSION} \
        -v $(pwd)/generate/.output:/usr/src/ \
        finbourne/lusid-sdk-gen-csharp:latest -- bash -c "cd /usr/src/sdk; dotnet dev-certs https --trust; dotnet pack -c Release; find . -name \"*.nupkg\" -exec dotnet publish {} \;"

publish-cicd SRC_DIR:
    echo "PACKAGE_VERSION to publish: ${PACKAGE_VERSION}"
    dotnet dev-certs https --trust
    set -e
    dotnet pack -c Release /p:AssemblyVersion=${ASSEMBLY_VERSION} /p:PackageVersion=${PACKAGE_VERSION} /p:PackageId=${PACKAGE_NAME} {{SRC_DIR}}
    find {{SRC_DIR}} -name "*.nupkg" -type f -exec \
        dotnet nuget push {} --skip-duplicate \
            --source ${REPO_URL} \
            --api-key ${API_KEY} \;

publish-to SRC_DIR OUT_DIR:
    echo "PACKAGE_VERSION to publish: ${PACKAGE_VERSION}"
    set +e
    dotnet dev-certs https --trust
    set  -e
    dotnet pack -c Release /p:AssemblyVersion=${ASSEMBLY_VERSION} /p:PackageVersion=${PACKAGE_VERSION} /p:PackageId=${PACKAGE_NAME} {{SRC_DIR}}/sdk
    find {{SRC_DIR}} -name "*.nupkg" -type f -exec cp {} {{OUT_DIR}} \;

generate-and-publish TARGET_DIR:
    @just generate {{TARGET_DIR}}
    @just publish-only

generate-and-publish-local:
    @just generate-local
    @just publish-only-local

generate-and-publish-cicd OUT_DIR:
    @just generate-cicd {{OUT_DIR}}
    @just publish-cicd {{OUT_DIR}}

add-tests SDK_DIR:
    bash tests/add-tests.sh {{SDK_DIR}}

test-cicd SDK_DIR:
    @just add-tests {{SDK_DIR}}
    bash tests/run-tests.sh {{SDK_DIR}}/sdk "${PROJECT_NAME}/${PROJECT_NAME}.csproj"

test-local:
    @just generate-local
    @just add-tests {{justfile_directory()}}/generate/.output
    FBN_LUSID_URL=${FBN_BASE_URL}/api \
        FBN_LUMINESCE_URL=${FBN_BASE_URL}/honeycomb \
        FBN_IDENTITY_URL=${FBN_BASE_URL}/identity \
        FBN_ACCESS_URL=${FBN_BASE_URL}/access \
        FBN_DRIVE_URL=${FBN_BASE_URL}/drive \
        FBN_NOTIFICATIONS_URL=${FBN_BASE_URL}/notifications \
        FBN_SCHEDULER_URL=${FBN_BASE_URL}/scheduler2 \
        FBN_INSIGHTS_URL=${FBN_BASE_URL}/insights \
        FBN_CONFIGURATION_URL=${FBN_BASE_URL}/configuration \
        FBN_WORKFLOW_URL=${FBN_BASE_URL}/workflow \
        FBN_HORIZON_URL=${FBN_BASE_URL}/horizon \
        bash tests/run-tests.sh generate/.output/sdk "${PROJECT_NAME}/${PROJECT_NAME}.csproj"

test-local-docker:
    @just generate-local
    @just add-tests {{justfile_directory()}}/generate/.output
    cp {{justfile_directory()}}/tests/run-tests.sh generate/.output/sdk/run-tests.sh
    docker run --rm \
        -e PACKAGE_NAME=${PACKAGE_NAME} \
        -e PROJECT_NAME=${PROJECT_NAME} \
        -e FBN_APP_NAME=${FBN_APP_NAME} \
        -e FBN_TOKEN_URL=${FBN_TOKEN_URL} \
        -e FBN_USERNAME=${FBN_USERNAME} \
        -e FBN_PASSWORD=${FBN_PASSWORD} \
        -e FBN_CLIENT_ID=${FBN_CLIENT_ID} \
        -e FBN_CLIENT_SECRET=${FBN_CLIENT_SECRET} \
        -e FBN_ACCESS_TOKEN=${FBN_ACCESS_TOKEN} \
        -e FBN_LUSID_URL=${FBN_BASE_URL}/api \
        -e FBN_LUMINESCE_URL=${FBN_BASE_URL}/honeycomb \
        -e FBN_IDENTITY_URL=${FBN_BASE_URL}/identity \
        -e FBN_ACCESS_URL=${FBN_BASE_URL}/access \
        -e FBN_LUSID_DRIVE_URL=${FBN_BASE_URL}/drive \
        -e FBN_NOTIFICATIONS_URL=${FBN_BASE_URL}/notifications \
        -e FBN_SCHEDULER_URL=${FBN_BASE_URL}/scheduler2 \
        -e FBN_INSIGHTS_URL=${FBN_BASE_URL}/insights \
        -e FBN_CONFIGURATION_URL=${FBN_BASE_URL}/configuration \
        -e FBN_WORKFLOW_URL=${FBN_BASE_URL}/workflow \
        -e FBN_HORIZON_URL=${FBN_BASE_URL}/horizon \
        -w /usr/src/sdk \
        -v $(pwd)/generate/.output/sdk:/usr/src/sdk \
        mcr.microsoft.com/dotnet/sdk:6.0 bash run-tests.sh /usr/src/sdk "${PROJECT_NAME}/${PROJECT_NAME}.csproj"

get-templates:
    docker run --rm \
        -v {{justfile_directory()}}/.templates:/usr/src/out \
        finbourne/lusid-sdk-gen-csharp:latest author template -g csharp-netcore 
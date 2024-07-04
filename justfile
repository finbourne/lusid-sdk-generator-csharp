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
#    FBN_BASE_API_URL

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
export META_REQUEST_ID_HEADER_KEY := `echo ${META_REQUEST_ID_HEADER_KEY:-lusid-meta-requestid}`
export NUGET_PACKAGE_LOCATION     := `echo ${NUGET_PACKAGE_LOCATION:-~/.nuget/local-packages}`
export EXCLUDE_TESTS              := `echo ${EXCLUDE_TESTS:-false}`

swagger_path := "./swagger.json"

swagger_url := "https://example.lusid.com/api/swagger/v0/swagger.json"

get-swagger:
    echo {{swagger_url}}
    curl -s {{swagger_url}} > swagger.json

build-docker-images: 
    docker build -t finbourne/lusid-sdk-gen-csharp:latest --ssh default=$SSH_AUTH_SOCK --platform linux/amd64/v2 -f Dockerfile .

generate-local:
    envsubst < generate/config-template.json > generate/.config.json
    rm -r generate/.output || true
    docker run \
        -e JAVA_OPTS="-Dlog.level=error" \
        -e APPLICATION_NAME=${APPLICATION_NAME} \
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

    ./generate/generate.sh ./generate ./generate/.output {{swagger_path}} .config.json
    rm -f generate/.output/.openapi-generator-ignore

    # need to remove the created content before copying over the top of it.
    # this prevents deleted content from hanging around indefinitely.
    rm -rf {{TARGET_DIR}}/sdk/${APPLICATION_NAME}
    rm -rf {{TARGET_DIR}}/sdk/docs
    
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

test-local:
    @just generate-local
    docker run \
        -e PROJECT_NAME=${PROJECT_NAME} \
        -e FBN_API_TEST_APP_NAME=${APPLICATION_NAME} \
        -e GIT_REPO_NAME=${GIT_REPO_NAME} \
        -e FBN_TOKEN_URL=${FBN_TOKEN_URL} \
        -e FBN_ACCESS_TOKEN=${FBN_ACCESS_TOKEN} \
        -e FBN_USERNAME=${FBN_USERNAME} \
        -e FBN_CLIENT_ID=${FBN_CLIENT_ID} \
        -e FBN_CLIENT_SECRET=${FBN_CLIENT_SECRET} \
        -e FBN_LUSID_API_URL=${FBN_BASE_API_URL}/api \
        -e FBN_LUMI_API_URL=${FBN_BASE_API_URL}/honeycomb \
        -e FBN_LUSID_IDENTITY_API_URL=${FBN_BASE_API_URL}/identity \
        -e FBN_LUSID_ACCESS_API_URL=${FBN_BASE_API_URL}/access \
        -e FBN_LUSID_DRIVE_API_URL=${FBN_BASE_API_URL}/drive \
        -e FBN_NOTIFICATIONS_API_URL=${FBN_BASE_API_URL}/notifications \
        -e FBN_SCHEDULER_API_URL=${FBN_BASE_API_URL}/scheduler2 \
        -e FBN_INSIGHTS_API_URL=${FBN_BASE_API_URL}/insights \
        -e FBN_CONFIGURATION_API_URL=${FBN_BASE_API_URL}/configuration \
        -e FBN_APP_NAME=${FBN_APP_NAME} \
        -e FBN_PASSWORD=${FBN_PASSWORD} \
        -w /usr/src/tests \
        -v $(pwd)/generate/.output/sdk/:/usr/src/sdk/ \
        -v $(pwd)/tests/:/usr/src/tests/ \
        mcr.microsoft.com/dotnet/sdk:6.0 bash -- run-tests.sh

get-templates:
    docker run \
        -v {{justfile_directory()}}/.templates:/usr/src/out \
        finbourne/lusid-sdk-gen-csharp:latest author template -g csharp-netcore 
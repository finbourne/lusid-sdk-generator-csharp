export PACKAGE_NAME               := `echo ${PACKAGE_NAME:-Lusid.Sdk}`
export PROJECT_NAME               := `echo ${PROJECT_NAME:-Lusid.Sdk}`
export PACKAGE_VERSION            := `echo ${PACKAGE_VERSION:-2.0.0}`
export APPLICATION_NAME           := `echo ${APPLICATION_NAME:-lusid}`
export META_REQUEST_ID_HEADER_KEY := `echo ${META_REQUEST_ID_HEADER_KEY:-lusid-meta-requestid}`
export NUGET_PACKAGE_LOCATION     := `echo ${NUGET_PACKAGE_LOCATION:-~/.nuget/local-packages}`

swagger_path := "./swagger.json"

swagger_url := "https://example.lusid.com/api/swagger/v0/swagger.json"

get-swagger:
    echo {{swagger_url}}
    curl -s {{swagger_url}} > swagger.json

build-docker-images: 
    docker build --platform linux/amd64 -t finbourne/lusid-sdk-gen-csharp:latest --ssh default=$SSH_AUTH_SOCK -f Dockerfile generate
    docker build --platform linux/amd64 -t finbourne/lusid-sdk-pub-csharp:latest -f publish/Dockerfile publish

generate-local:
    mkdir -p /tmp/${PROJECT_NAME}_${PACKAGE_VERSION}
    envsubst < generate/config-template.json > generate/.config.json
    docker run \
        -e JAVA_OPTS="-Dlog.level=error" \
        -e APPLICATION_NAME=${APPLICATION_NAME} \
        -e META_REQUEST_ID_HEADER_KEY=${META_REQUEST_ID_HEADER_KEY} \
        -e PACKAGE_VERSION=${PACKAGE_VERSION} \
        -v $(pwd)/generate/:/usr/src/generate/ \
        -v $(pwd)/generate/.openapi-generator-ignore:/usr/src/generate/.output/.openapi-generator-ignore \
        -v $(pwd)/{{swagger_path}}:/tmp/swagger.json \
        lusid-sdk-gen-csharp:latest -- ./generate/generate.sh ./generate ./generate/.output /tmp/swagger.json .config.json
    rm -f generate/.output/.openapi-generator-ignore
    
generate TARGET_DIR:
    @just generate-local
    
    # need to remove the created content before copying over the top of it.
    # this prevents deleted content from hanging around indefinitely.
    rm -rf {{TARGET_DIR}}/sdk/lusid
    rm -rf {{TARGET_DIR}}/sdk/docs
    
    mv -R generate/.output/* {{TARGET_DIR}}

generate-cicd TARGET_DIR:
    mkdir -p /tmp/${PROJECT_NAME}_${PACKAGE_VERSION}
    envsubst < generate/config-template.json > generate/.config.json
    cp ./generate/.openapi-generator-ignore ./generate/.output/.openapi-generator-ignore

    ./generate/generate.sh ./generate ./generate/.output /tmp/swagger.json .config.json

    # need to remove the created content before copying over the top of it.
    # this prevents deleted content from hanging around indefinitely.
    rm -rf {{TARGET_DIR}}/sdk/lusid
    rm -rf {{TARGET_DIR}}/sdk/docs
    
    mv -R generate/.output/* {{TARGET_DIR}}

publish-only-local:
    docker run \
        -e PACKAGE_VERSION=${PACKAGE_VERSION} \
        -v $(pwd)/generate/.output:/usr/src \
        lusid-sdk-pub-csharp:latest -- "cd /usr/src/sdk; dotnet pack -c Release"
    mkdir -p ${NUGET_PACKAGE_LOCATION}
    find . -name "*.nupkg" -exec cp {} ${NUGET_PACKAGE_LOCATION} \;

publish-only:
    docker run \
        -e PACKAGE_VERSION=${PACKAGE_VERSION} \
        -v $(pwd)/generate/.output:/usr/src \
        lusid-sdk-pub-csharp:latest -- "cd /usr/src/sdk; ls; dotnet pack -c Release; find . -name \"*.nupkg\" -exec dotnet publish {} \;"

generate-and-publish TARGET_DIR:
    @just generate {{TARGET_DIR}}
    @just publish-only

generate-and-publish-local:
    @just generate-local
    @just publish-only-local

test:
    ./test/test.sh

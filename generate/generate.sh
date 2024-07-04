#!/bin/bash

set -EeTuo pipefail

failure() {
    local lineno=$1
    local msg=$2
    echo "Failed at $lineno: $msg"
}
trap 'failure ${LINENO} "$BASH_COMMAND"' ERR

gen_root=$1
output_folder=$2
swagger_file=$3
config_file_name=$4
sdk_output_folder=$output_folder/sdk
JAVA_OPTS=${JAVA_OPTS:--Dlog.level=info}

if [[ -z $config_file_name || ! -f $gen_root/$config_file_name ]] ; then
    echo "[INFO] '$config_file_name' not found, using default config.json"
    config_file_name=config.json
fi

echo "[INFO] root generation : $gen_root"
echo "[INFO] output folder   : $output_folder"
echo "[INFO] swagger file    : $swagger_file"
echo "[INFO] config file     : $config_file_name"
echo "[INFO] application     : ${APPLICATION_NAME}"
echo "[INFO] header_key      : ${META_REQUEST_ID_HEADER_KEY}"
echo "[INFO] exclude tests   : ${EXCLUDE_TESTS}"

ignore_file_name=.openapi-generator-ignore
config_file=$gen_root/$config_file_name
ignore_file=$output_folder/$ignore_file_name

app_name=$(cat $config_file | jq -r .packageName)

#   remove all previously generated files
shopt -s extglob 
echo "[INFO] removing previous sdk:"
rm -rf $sdk_output_folder/$app_name/!(Utilities|*.csproj)
shopt -u extglob 

# ignore files
mkdir -p $sdk_output_folder
cp $ignore_file $sdk_output_folder

# set versions
# sdk_version=$(cat $swagger_file | jq -r '.info.version')
# sdk_version=${PACKAGE_VERSION}
# echo "[INFO] setting version to ${PACKAGE_VERSION}"

# cat $config_file | jq -r --arg SDK_VERSION "$sdk_version" '.packageVersion |= $SDK_VERSION' > temp && mv temp $config_file
# sed -i 's/<Version>.*<\/Version>/<Version>'$sdk_version'<\/Version>/g' $sdk_output_folder/$app_name/$app_name.csproj

GENERATE_VALIDATION_EXCEPTION_CODE=""
if grep -q LusidValidationProblemDetails $swagger_file; then GENERATE_VALIDATION_EXCEPTION_CODE=",generate_validation_exception_code=true" ; fi

echo "[INFO] generating sdk version: ${PACKAGE_VERSION}"

#java -jar swagger-codegen-cli.jar swagger-codegen-cli help
java ${JAVA_OPTS} -jar /opt/openapi-generator/modules/openapi-generator-cli/target/openapi-generator-cli.jar generate \
    -i $swagger_file \
    -g csharp-netcore \
    -o $sdk_output_folder \
    -c $config_file \
    -t $gen_root/templates \
    --additional-properties=application=${APPLICATION_NAME},meta_request_id_header_key=${META_REQUEST_ID_HEADER_KEY}${GENERATE_VALIDATION_EXCEPTION_CODE} \
	--type-mappings dateorcutlabel=DateTimeOrCutLabel \
    --type-mappings double=decimal \
    --git-repo-id=${GIT_REPO_NAME}

rm -rf $sdk_output_folder/.openapi-generator
rm -f $sdk_output_folder/.openapi-generator-ignore
rm -f $sdk_output_folder/.gitignore
rm -f $sdk_output_folder/git_push.sh
rm -f $sdk_output_folder/README.md
# rm -rf $sdk_output_folder/src
rm -f $sdk_output_folder/swagger.json
rm -rf $sdk_output_folder/.github/

mkdir -p $output_folder/docs/
cp -R /tmp/docs/docs/* $output_folder/docs/
mkdir -p $output_folder/.github/
cp -R /tmp/workflows/github/* $output_folder/.github/

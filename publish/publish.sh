#!/bin/bash -e

if [[ (${#1} -eq 0) ]] ; then
    echo 
    echo "[ERROR] missing API key"
    exit 1
fi

if [[ (${#2} -eq 0) ]] ; then
    echo 
    echo "[ERROR] missing repo URL"
    exit 1
fi

api_key=$1
repo_url=$2

sdk_version=$(cat lusid.json | jq -r '.info.version')

# check the SDK builds
dotnet build sdk/Lusid.Sdk.sln

sed -i 's/<Version>.*<\/Version>/<Version>'$sdk_version'<\/Version>/g' sdk/Lusid.Sdk/Lusid.Sdk.csproj

echo "sdk_version=$sdk_version"

dotnet pack -c Release sdk/Lusid.Sdk/Lusid.Sdk.csproj

test="$(find sdk/Lusid.Sdk/bin/Release/Lusid* -type f -printf "%f")"

dotnet nuget push sdk/Lusid.Sdk/bin/Release/$test \
    --source $repo_url \
    --api-key $api_key

rm -f publish.sh
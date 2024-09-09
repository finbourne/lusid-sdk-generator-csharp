#!/bin/bash

set -eETuo pipefail

failure() {
  local lineno="$1"
  local msg="$2"
  echo "Failed at $lineno: $msg"
}
trap 'failure ${LINENO} "$BASH_COMMAND"' ERR

sdk_dir=$1

# copy the tests dir to the sdk dir
cp -r tests/Tests "$sdk_dir/sdk"

# make all necessary replacements
upper_case_application_name="$(echo "$APPLICATION_SHORT_NAME" | tr '[:lower:]' '[:upper:]')"; \
    find "$sdk_dir/sdk/Tests" -type f -exec sed -i -e "s/TO_BE_REPLACED_UPPER/${upper_case_application_name}/g" {} \;
prefix_upper="$(echo "$APPLICATION_NAME" | cut -d'-' -f1 | tr '[:lower:]' '[:upper:]')"; \
    find "$sdk_dir/sdk/Tests" -type f -exec sed -i -e "s/PREFIX_UPPER/${prefix_upper}/g" {} \;
prefix_lower="$(echo "$APPLICATION_NAME" | cut -d'-' -f1 | tr '[:upper:]' '[:lower:]')"; \
    find "$sdk_dir/sdk/Tests" -type f -exec sed -i -e "s/PREFIX_LOWER/${prefix_lower}/g" {} \;
find "$sdk_dir/sdk/Tests" -type f -exec sed -i -e "s/TO_BE_REPLACED_PROJECT_NAME/${PROJECT_NAME}/g" {} \;
find "$sdk_dir/sdk/Tests" -type f -exec sed -i -e "s/TO_BE_REPLACED_LOWER/${APPLICATION_SHORT_NAME}/g" {} \;
find "$sdk_dir/sdk/Tests" -type f -exec sed -i -e "s/TEST_API/${TEST_API}/g" {} \;
# if the test method has an argument, ASYNC_TEST_METHOD_WITH_EXTRA_ARG needs a ', ' at the end
if echo "${ASYNC_TEST_METHOD}" | grep -q "([a-zA-Z]"; then
    find "$sdk_dir/sdk/Tests" -type f -exec sed -i -e "s/ASYNC_TEST_METHOD_WITH_EXTRA_ARG/${ASYNC_TEST_METHOD}, /g" {} \;
else
    find "$sdk_dir/sdk/Tests" -type f -exec sed -i -e "s/ASYNC_TEST_METHOD_WITH_EXTRA_ARG/${ASYNC_TEST_METHOD}/g" {} \;
fi
find "$sdk_dir/sdk/Tests" -type f -exec sed -i -e "s/ASYNC_TEST_METHOD/${ASYNC_TEST_METHOD}/g" {} \;
# if the test method has an argument, TEST_METHOD_WITH_EXTRA_ARG needs a ', ' at the end
if echo "${TEST_METHOD}" | grep -q "([a-zA-Z]"; then
    find "$sdk_dir/sdk/Tests" -type f -exec sed -i -e "s/TEST_METHOD_WITH_EXTRA_ARG/${TEST_METHOD}, /g" {} \;
else
    find "$sdk_dir/sdk/Tests" -type f -exec sed -i -e "s/TEST_METHOD_WITH_EXTRA_ARG/${TEST_METHOD}/g" {} \;
fi
find "$sdk_dir/sdk/Tests" -type f -exec sed -i -e "s/TEST_METHOD/${TEST_METHOD}/g" {} \;
#!/bin/bash

set -euo pipefail

dir=$1
main_project=$2

# add the project to the solution
dotnet sln "$dir" add "$dir/Tests/Finbourne.Sdk.Extensions.Tests.csproj"

# add the project as a reference to the tests project
dotnet add "$dir/Tests" reference "$dir/$main_project"

cat > "$dir/Tests/Usings.cs" <<EOF
global using ${PROJECT_NAME};
global using Client = ${PROJECT_NAME}.Client;
global using ${PROJECT_NAME}.Client;
global using ${PROJECT_NAME}.Model;
global using ${PROJECT_NAME}.Extensions;
EOF

dotnet test "$dir"

#!/bin/bash -e

# add the project to the solution
dotnet sln add ../sdk/${PROJECT_NAME}

# add the project as a reference to the tests project
dotnet add Tests/ reference ../sdk/${PROJECT_NAME}

cat > Tests/Usings.cs <<EOF
global using ${PROJECT_NAME};
global using Client = ${PROJECT_NAME}.Client;
global using ${PROJECT_NAME}.Client;
global using ${PROJECT_NAME}.Model;
global using ${PROJECT_NAME}.Extensions;
EOF

dotnet test

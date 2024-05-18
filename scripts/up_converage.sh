#!/bin/bash

curl -s https://codecov.io/bash > codecov
chmod +x codecov
./codecov -f "$(Build.SourcesDirectory)/CodeCoverage/Cobertura.xml" -t $1
#!/bin/bash

cd test/Unit/ || exit

for i in *.Tests ; do 
  echo "### Executing Tests for $i:"
  
  time dotnet test "$i" --no-build \
    --collect:"XPlat Code Coverage" \
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile="examples/*" \
    > /tmp/freeredis-test.log
    
  if [[ $? -ne 0 ]] ; then
    echo "Test Run Failed."
    cat /tmp/freeredis-test.log
    exit 1
  fi
  echo "Test Run Successful."
done 
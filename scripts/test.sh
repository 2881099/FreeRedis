#!/bin/bash

# Setup environment variables

while read name ports image; do
    key="DOCKER_HOST_$name"
    # 0.0.0.0:1234->9876/tcp,first
    tempPort=$(cut -d'-' -f1 <<<"$ports")
    vr="$key=$tempPort"
    echo "$vr"
    export "$vr"
done <<<$(docker ps --filter "status="running"" --format "{{.Names}}\t{{.Ports}}\t{{.Image}}")

#echo $(printenv DOCKER_HOST_redis_auth)
#echo $(printenv DOCKER_HOST_redis_single)

echo "setup is finished"

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
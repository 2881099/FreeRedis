#!/bin/bash
dotnet sln remove examples/*
dotnet restore -v quiet "FreeRedis.sln"
dotnet build /clp:ErrorsOnly -v quiet "FreeRedis.sln"
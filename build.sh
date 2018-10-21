#!/bin/bash

SLN=./src/git-istage.sln
BIN=./bin/
CONFIG=release

dotnet build $SLN -c $CONFIG -o=$BIN /nologo

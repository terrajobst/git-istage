#!/bin/bash

SLN=./src/git-istage.sln
CONFIG=release

dotnet build $SLN -c $CONFIG /nologo

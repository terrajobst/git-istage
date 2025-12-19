#!/bin/bash

SLN=./src/git-istage.slnx
CONFIG=release

dotnet build $SLN -c $CONFIG /nologo

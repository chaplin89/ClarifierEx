# Overview

**This is only a PoC and it needs some major refactoring.**

ClarifierEx is a deobfuscator for ConfuserEx.
ConfuserEx is an obfuscator that support the following features:
  - Constants ofbuscation
  - Anti-dump
  - Anti-debug
  - Reference proxy
  - Anti-ildasm
  - Anti-tamper
  - Control flow obfuscation
  - Invalid metadata
  - Resource protection
  - Name obfuscation
  - Packer

## Supported protections
  - Anti-dump
  - Anti-debug

## Partially supported protections
These protection are only supported with standard configuration of ConfuserEx (see [wiki](https://github.com/chaplin89/ClarifierEx/wiki/ConfuserEx-settings) ):

  - Constants obfuscation
  - Reference proxy

## Plan to support other protections
Provided that the goal of a deobfuscator is not to "win the competion for the best looking assemblies" but to ease the job of the reverse engineer, as long as there is at least a reverse engineering tool capable of handling the metadata malformation or the other protections, it's not important for ClarifierEx to support those protections. From this starting point, here follow the plan to add the other protections.

### High priority
  - Anti-tamper
  - Control flow obfuscation
  - Packer

### Medium priority
  - Resource protection

### Low Priority
  - Name obfuscation

### Very Low Priority
  - Anti-ildasm
  - Invalid metadata

# Compiling
## Requirements
Visual Studio 2015
## Steps
1. Clone the repository and launch "build.bat" in order to build ConfuserEx 
2. Open Clarifier.sln and build the solution

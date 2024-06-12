# Use xUnit Testing Framework

## Context and Problem Statement

A single testing framework needs to be adopted by the project so that we can standardize our testing framework

## Decision Drivers

* Community Adoption
    * How much market share does the testing framework have
* Integration with 3rd party tools / IDEs

## Considered Options

* [MS Test](https://github.com/microsoft/testfx)
    * Testing framework developed by Microsoft, phased out on MS OSS projects in favor of xUnit
* [xUnit](https://xunit.net/)
    * Commonly used in recent .NET projects, including Microsoft in the libraries they've developed and adopted in the
      past 3 years
* [NUnit](https://nunit.org/)
    * Well established framework
    * A port of the [JUnit](https://junit.org/junit5/) java framework
    * Somewhat lessening in popularity of late

## Decision Outcome

Chosen option: xUnit because it is an industry standard and is receiving a lot of development focus of late.


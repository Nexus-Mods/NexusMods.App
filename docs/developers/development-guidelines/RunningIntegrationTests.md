# Running Integration Tests

The Nexus Mods app has certain tests marked as "integration tests", these are tests that require a valid copy of a game
in order to perform certain tests. These tests are separate from unit tests that attempt to mock various parts of the game
or introduce "fake game" data for testing. 

## Overview

The integration tests run by storing copies of game files in `.nx` archives hosted in a "game images" folder. This folder
has the structure of `{root}/{Store}/{LocatorId}.nx`. So for steam these files are in `{root}/Steam/1234567890.nx` for Manifest ID
`1234567890`.

During tests a custom `IFileSystem` is created that uses these game images as a base, and stores any modified files in memory, 
each test is then given a clean copy of the game files. Each test can run whatever operations it needs to run, and then
validate behavior against actual game files. Since NMA uses this `IFileSystem` abstraction across the entire app, it is possible
to use these game images as-is without extracting any files to disk. Tests can even be created to simulate a disk state
that does not match the reported Steam state. 

## Setup
In order to run the integration tests for a given game you must first create the game images. For steam this is done by
running the CLI command `steam manifest pack-game -a "{Game Name}" -o "{ImagesRoot}"`. Note that "Game Name" is the english
name of the game, such as "Skyrim Special Edition". The output folder is the base folder, not the Steam sub-folder. This
CLI command will download all the manifests for the given game and pack the game files into `.nx` archives. Next you must
specify the root folder for tests, via the environment variable `NMA_INTEGRATION_BASE_PATH` environment variable.

## Running Tests

Now simply navigate to a specific integration test folder and run `dotnet run` to run the tests via tUnit. The Integration tests 
will not run at all if the environment variable `NMA_INTEGRATION_BASE_PATH` is not set, and will error if the correct game
images are not present. This silent failure with the environment variable is intentional, so that CI runs in various environments
without failures and without having specify complex filters in the test runners.  The NMA project hosts all tgame images
on the test runner for the integration tests, speak with the app developers if there are additional manifests requried for testing.

## Create new tests
To create new tests, create a class that inherits from `AGameIntegrationTest`. Specify which games to run the test on
via attributes attached to the class. These attributes then generate data that is used to parameterize the test. Be liberal
with the creation of new test attributes, as they are an easy extension point for creating new inputs and for verifying
behavior across multiple games and platforms.

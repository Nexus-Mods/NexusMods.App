using FluentAssertions;
using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.Networking.NexusWebApi.Tests.Types.V2;

public class FileUidTests
{
    [Fact]
    public void UidForFile_IsCorrectSize()
    {
        // Arrange
        unsafe
        {
            // UidForFile does an unsafe cast in FromUlong and AsUlong
            // This test ensures nobody tampers with the size of the struct
            // or its components; ensuring those unsafe casts are safe.
            sizeof(FileUid).Should().Be(8);
            sizeof(FileId).Should().Be(4);
            sizeof(NexusModsGameId).Should().Be(4);
        }
    }
    
    [Theory]
    [InlineData(1704U, 405U, "7318624272789")]
    [InlineData(1704U, 407U, "7318624272791")]
    [InlineData(1704U, 406U, "7318624272790")]
    [InlineData(1704U, 5564U, "7318624277948")]
    [InlineData(1704U, 5565U, "7318624277949")]
    [InlineData(1704U, 163337U, "7318624435721")]
    [InlineData(1704U, 163338U, "7318624435722")]
    [InlineData(1704U, 296267U, "7318624568651")]
    [InlineData(1704U, 296268U, "7318624568652")]
    [InlineData(3333U, 1U, "14315125997569")]
    [InlineData(3333U, 2U, "14315125997570")]
    [InlineData(3333U, 41002U, "14315126038570")]
    [InlineData(3333U, 4U, "14315125997572")]
    public void FromV2Api_ValidInput_ReturnsCorrectUidForFile(uint expectedGameId, uint expectedFileId, string uidString)
    {
        // Act
        var result = FileUid.FromV2Api(uidString);

        // Assert
        result.GameId.Should().Be((NexusModsGameId)expectedGameId);
        result.FileId.Should().Be((FileId)expectedFileId);
    }

    [Fact]
    public void FromV2Api_InvalidInput_ThrowsFormatException()
    {
        // Arrange
        var invalidUid = "not a number";

        // Act & Assert
        Action act = () => FileUid.FromV2Api(invalidUid);
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData(1704U, 405U, 7318624272789UL)]
    [InlineData(1704U, 407U, 7318624272791UL)]
    [InlineData(1704U, 406U, 7318624272790UL)]
    [InlineData(3333U, 1U, 14315125997569UL)]
    [InlineData(3333U, 2U, 14315125997570UL)]
    public void AsUlong_ReturnsCorrectValue(uint gameId, uint fileId, ulong expectedUlong)
    {
        // Arrange
        var uidForFile = new FileUid((FileId)fileId, (NexusModsGameId)gameId);

        // Act
        var result = uidForFile.AsUlong;

        // Assert
        result.Should().Be(expectedUlong);
    }

    [Theory]
    [InlineData(7318624272789UL, 1704U, 405U)]
    [InlineData(7318624272791UL, 1704U, 407U)]
    [InlineData(7318624272790UL, 1704U, 406U)]
    [InlineData(14315125997569UL, 3333U, 1U)]
    [InlineData(14315125997570UL, 3333U, 2U)]
    public void FromUlong_ReturnsCorrectUidForFile(ulong input, uint expectedGameId, uint expectedFileId)
    {
        // Act
        var result = FileUid.FromUlong(input);

        // Assert
        result.GameId.Should().Be((NexusModsGameId)expectedGameId);
        result.FileId.Should().Be((FileId)expectedFileId);
    }

    [Theory]
    [InlineData(1704U, 405U)]
    [InlineData(3333U, 1U)]
    public void RoundTrip_UlongConversion_PreservesValues(uint gameId, uint fileId)
    {
        // Arrange
        var original = new FileUid((FileId)fileId, (NexusModsGameId)gameId);

        // Act
        var asUlong = original.AsUlong;
        var roundTripped = FileUid.FromUlong(asUlong);

        // Assert
        roundTripped.Should().Be(original);
    }
}


/*
    Deriveration of test cases.
 
    Original Request(s):
    
    ```
    query ModFiles($modId: ID!, $gameId: ID!) {
       modFiles(modId: $modId, gameId: $gameId) {
           fileId
           uid
       }
    }
    ```
   
    Input:
    ```
    {
       "modId": 1,
       "gameId": "1704"
    }
   ```
    
    Response:
    ```json
    {
       "data": {
           "modFiles": [
               {
                   "fileId": 405,
                   "uid": "7318624272789"
               }
           ]
       }
    }
    ```
    
    Input:
    ```
    {
       "modId": 2,
       "gameId": "1704"
    }
   ```
    
    Response:
    ```json
    {
       "data": {
           "modFiles": [
               {
                   "fileId": 407,
                   "uid": "7318624272791"
               }
           ]
       }
    }
    ```
 
    Input:
    ```
    {
       "modId": 3,
       "gameId": "1704"
    }
   ```
    
    Response:
    ```json
    {
       "data": {
           "modFiles": [
               {
                   "fileId": 406,
                   "uid": "7318624272790"
               }
           ]
       }
    }
    ```
    
    Input:
    ```
    {
       "modId": 1000,
       "gameId": "1704"
    }
   ```
    
    Response:
    ```json
    {
       "data": {
           "modFiles": [
               {
                   "fileId": 5564,
                   "uid": "7318624277948"
               },
               {
                   "fileId": 5565,
                   "uid": "7318624277949"
               },
               {
                   "fileId": 163337,
                   "uid": "7318624435721"
               },
               {
                   "fileId": 163338,
                   "uid": "7318624435722"
               },
               {
                   "fileId": 296267,
                   "uid": "7318624568651"
               },
               {
                   "fileId": 296268,
                   "uid": "7318624568652"
               }
           ]
       }
    }
    ```
    
    Input:
    ```
    {
      "modId": 1,
      "gameId": "3333"
    }
    ```

    Response:
    ```json
    {
       "data": {
           "modFiles": [
               {
                   "fileId": 1,
                   "uid": "14315125997569"
               },
               {
                   "fileId": 2,
                   "uid": "14315125997570"
               },
               {
                   "fileId": 41002,
                   "uid": "14315126038570"
               }
           ]
       }
    }
    ```
    
    Input:
    ```
    {
        "modId": 1,
        "gameId": "3333"
    }
    ```

    Response:
    ```json
    {
        "data": {
            "modFiles": [
                {
                    "fileId": 1,
                    "uid": "14315125997569"
                },
                {
                    "fileId": 2,
                    "uid": "14315125997570"
                },
                {
                    "fileId": 41002,
                    "uid": "14315126038570"
                }
            ]
        }
    }
    ```
    
    Input:
    ```
    {
       "modId": 3,
       "gameId": "3333"
    }
    ```
   
   Response:
    ```json
    {
       "data": {
           "modFiles": [
               {
                   "fileId": 4,
                   "uid": "14315125997572"
               }
           ]
       }
    }
*/

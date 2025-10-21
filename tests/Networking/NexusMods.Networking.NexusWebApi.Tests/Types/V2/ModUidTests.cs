using FluentAssertions;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.Networking.NexusWebApi.Tests.Types.V2;

public class ModUidTests
{
    [Fact]
    public void UidForMod_IsCorrectSize()
    {
        // Arrange
        unsafe
        {
            // UidForMod does an unsafe cast in FromUlong and AsUlong
            // This test ensures nobody tampers with the size of the struct
            // or its components; ensuring those unsafe casts are safe.
            sizeof(ModUid).Should().Be(8);
            sizeof(GameId).Should().Be(4);
            sizeof(FileId).Should().Be(4);
        }
    }
    
    [Theory]
    [InlineData(1704U, 130248U, "7318624402632")]
    [InlineData(1704U, 130167U, "7318624402551")]
    [InlineData(1704U, 130246U, "7318624402630")]
    [InlineData(1704U, 130245U, "7318624402629")]
    [InlineData(1704U, 130243U, "7318624402627")]
    [InlineData(1704U, 130244U, "7318624402628")]
    [InlineData(1704U, 130242U, "7318624402626")]
    [InlineData(1704U, 130240U, "7318624402624")]
    [InlineData(1704U, 130241U, "7318624402625")]
    [InlineData(1704U, 130191U, "7318624402575")]
    [InlineData(1704U, 130239U, "7318624402623")]
    [InlineData(1704U, 129994U, "7318624402378")]
    [InlineData(1704U, 130237U, "7318624402621")]
    [InlineData(1704U, 130238U, "7318624402622")]
    [InlineData(1704U, 130234U, "7318624402618")]
    [InlineData(1704U, 130235U, "7318624402619")]
    [InlineData(1704U, 130230U, "7318624402614")]
    [InlineData(1704U, 130233U, "7318624402617")]
    [InlineData(1704U, 130232U, "7318624402616")]
    [InlineData(1704U, 130231U, "7318624402615")]
    [InlineData(2500U, 76U, "10737418240076")]
    [InlineData(2500U, 75U, "10737418240075")]
    [InlineData(2500U, 74U, "10737418240074")]
    [InlineData(2500U, 73U, "10737418240073")]
    [InlineData(2500U, 72U, "10737418240072")]
    [InlineData(2500U, 70U, "10737418240070")]
    [InlineData(2500U, 69U, "10737418240069")]
    [InlineData(2500U, 68U, "10737418240068")]
    [InlineData(2500U, 67U, "10737418240067")]
    [InlineData(2500U, 66U, "10737418240066")]
    [InlineData(2500U, 65U, "10737418240065")]
    [InlineData(2500U, 64U, "10737418240064")]
    [InlineData(2500U, 63U, "10737418240063")]
    [InlineData(2500U, 62U, "10737418240062")]
    [InlineData(2500U, 60U, "10737418240060")]
    [InlineData(2500U, 59U, "10737418240059")]
    [InlineData(2500U, 58U, "10737418240058")]
    [InlineData(2500U, 57U, "10737418240057")]
    [InlineData(2500U, 56U, "10737418240056")]
    [InlineData(2500U, 55U, "10737418240055")]
    public void FromV2Api_ValidInput_ReturnsCorrectUidForMod(uint expectedGameId, uint expectedModId, string uidString)
    {
        // Act
        var result = ModUid.FromV2Api(uidString);

        // Assert
        result.GameId.Should().Be((GameId)expectedGameId);
        result.ModId.Should().Be((ModId)expectedModId);
        
        // Assert round trip
        var newString = result.ToV2Api();
        newString.Should().Be(uidString);
    }

    [Fact]
    public void FromV2Api_InvalidInput_ThrowsFormatException()
    {
        // Arrange
        var invalidUid = "not a number";

        // Act & Assert
        Action act = () => ModUid.FromV2Api(invalidUid);
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData(1704U, 130248U, 7318624402632UL)]
    [InlineData(1704U, 130167U, 7318624402551UL)]
    [InlineData(1704U, 130246U, 7318624402630UL)]
    [InlineData(1704U, 130245U, 7318624402629UL)]
    [InlineData(1704U, 130243U, 7318624402627UL)]
    [InlineData(2500U, 76U, 10737418240076UL)]
    [InlineData(2500U, 75U, 10737418240075UL)]
    [InlineData(2500U, 74U, 10737418240074UL)]
    [InlineData(2500U, 73U, 10737418240073UL)]
    [InlineData(2500U, 72U, 10737418240072UL)]
    public void AsUlong_ReturnsCorrectValue(uint gameId, uint modId, ulong expectedUlong)
    {
        // Arrange
        var uidForMod = new ModUid(ModId.From(modId), GameId.From(gameId));

        // Act
        var result = uidForMod.AsUlong;

        // Assert
        result.Should().Be(expectedUlong);
    }

    [Theory]
    [InlineData(7318624402632UL, 1704U, 130248U)]
    [InlineData(7318624402551UL, 1704U, 130167U)]
    [InlineData(7318624402630UL, 1704U, 130246U)]
    [InlineData(7318624402629UL, 1704U, 130245U)]
    [InlineData(7318624402627UL, 1704U, 130243U)]
    [InlineData(10737418240076UL, 2500U, 76U)]
    [InlineData(10737418240075UL, 2500U, 75U)]
    [InlineData(10737418240074UL, 2500U, 74U)]
    [InlineData(10737418240073UL, 2500U, 73U)]
    [InlineData(10737418240072UL, 2500U, 72U)]
    public void FromUlong_ReturnsCorrectUidForMod(ulong input, uint expectedGameId, uint expectedModId)
    {
        // Act
        var result = ModUid.FromUlong(input);

        // Assert
        result.GameId.Should().Be((GameId)expectedGameId);
        result.ModId.Should().Be((ModId)expectedModId);
    }

    [Theory]
    [InlineData(1704U, 130248U)]
    [InlineData(2500U, 76U)]
    public void RoundTrip_UlongConversion_PreservesValues(uint gameId, uint modId)
    {
        // Arrange
        var original = new ModUid(ModId.From(modId), GameId.From(gameId));

        // Act
        var asUlong = original.AsUlong;
        var roundTripped = ModUid.FromUlong(asUlong);

        // Assert
        roundTripped.Should().Be(original);
    }
}


/*
    Deriveration of test cases.
 
    Original Request(s):
    
    ```
    query Mods {
       mods(filter: { gameId: { value: "1704", op: EQUALS } }) {
           nodes {
               gameId
               modId
               uid
           }
       }
    }
    ```
   
    ```
    query Mods {
       mods(filter: { gameId: { value: "2500", op: EQUALS } }) {
           nodes {
               gameId
               modId
               uid
           }
       }
    }
    ```
   
    Original Response(s):
    
    ```json
    {
       "data": {
           "mods": {
               "nodes": [
                   {
                       "gameId": 1704,
                       "modId": 130248,
                       "uid": "7318624402632"
                   },
                   {
                       "gameId": 1704,
                       "modId": 130167,
                       "uid": "7318624402551"
                   },
                   {
                       "gameId": 1704,
                       "modId": 130246,
                       "uid": "7318624402630"
                   },
                   {
                       "gameId": 1704,
                       "modId": 130245,
                       "uid": "7318624402629"
                   },
                   {
                       "gameId": 1704,
                       "modId": 130243,
                       "uid": "7318624402627"
                   },
                   {
                       "gameId": 1704,
                       "modId": 130244,
                       "uid": "7318624402628"
                   },
                   {
                       "gameId": 1704,
                       "modId": 130242,
                       "uid": "7318624402626"
                   },
                   {
                       "gameId": 1704,
                       "modId": 130240,
                       "uid": "7318624402624"
                   },
                   {
                       "gameId": 1704,
                       "modId": 130241,
                       "uid": "7318624402625"
                   },
                   {
                       "gameId": 1704,
                       "modId": 130191,
                       "uid": "7318624402575"
                   },
                   {
                       "gameId": 1704,
                       "modId": 130239,
                       "uid": "7318624402623"
                   },
                   {
                       "gameId": 1704,
                       "modId": 129994,
                       "uid": "7318624402378"
                   },
                   {
                       "gameId": 1704,
                       "modId": 130237,
                       "uid": "7318624402621"
                   },
                   {
                       "gameId": 1704,
                       "modId": 130238,
                       "uid": "7318624402622"
                   },
                   {
                       "gameId": 1704,
                       "modId": 130234,
                       "uid": "7318624402618"
                   },
                   {
                       "gameId": 1704,
                       "modId": 130235,
                       "uid": "7318624402619"
                   },
                   {
                       "gameId": 1704,
                       "modId": 130230,
                       "uid": "7318624402614"
                   },
                   {
                       "gameId": 1704,
                       "modId": 130233,
                       "uid": "7318624402617"
                   },
                   {
                       "gameId": 1704,
                       "modId": 130232,
                       "uid": "7318624402616"
                   },
                   {
                       "gameId": 1704,
                       "modId": 130231,
                       "uid": "7318624402615"
                   }
               ]
           }
       }
    }
    ```
    
    ```json
    {
       "data": {
           "mods": {
               "nodes": [
                   {
                       "gameId": 2500,
                       "modId": 76,
                       "uid": "10737418240076"
                   },
                   {
                       "gameId": 2500,
                       "modId": 75,
                       "uid": "10737418240075"
                   },
                   {
                       "gameId": 2500,
                       "modId": 74,
                       "uid": "10737418240074"
                   },
                   {
                       "gameId": 2500,
                       "modId": 73,
                       "uid": "10737418240073"
                   },
                   {
                       "gameId": 2500,
                       "modId": 72,
                       "uid": "10737418240072"
                   },
                   {
                       "gameId": 2500,
                       "modId": 70,
                       "uid": "10737418240070"
                   },
                   {
                       "gameId": 2500,
                       "modId": 69,
                       "uid": "10737418240069"
                   },
                   {
                       "gameId": 2500,
                       "modId": 68,
                       "uid": "10737418240068"
                   },
                   {
                       "gameId": 2500,
                       "modId": 67,
                       "uid": "10737418240067"
                   },
                   {
                       "gameId": 2500,
                       "modId": 66,
                       "uid": "10737418240066"
                   },
                   {
                       "gameId": 2500,
                       "modId": 65,
                       "uid": "10737418240065"
                   },
                   {
                       "gameId": 2500,
                       "modId": 64,
                       "uid": "10737418240064"
                   },
                   {
                       "gameId": 2500,
                       "modId": 63,
                       "uid": "10737418240063"
                   },
                   {
                       "gameId": 2500,
                       "modId": 62,
                       "uid": "10737418240062"
                   },
                   {
                       "gameId": 2500,
                       "modId": 60,
                       "uid": "10737418240060"
                   },
                   {
                       "gameId": 2500,
                       "modId": 59,
                       "uid": "10737418240059"
                   },
                   {
                       "gameId": 2500,
                       "modId": 58,
                       "uid": "10737418240058"
                   },
                   {
                       "gameId": 2500,
                       "modId": 57,
                       "uid": "10737418240057"
                   },
                   {
                       "gameId": 2500,
                       "modId": 56,
                       "uid": "10737418240056"
                   },
                   {
                       "gameId": 2500,
                       "modId": 55,
                       "uid": "10737418240055"
                   }
               ]
           }
       }
    }
    ```
 
*/

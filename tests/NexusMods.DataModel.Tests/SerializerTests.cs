using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using NexusMods.DataModel.JsonConverters;


namespace NexusMods.DataModel.Tests;

public class SerializerTests
{
    private readonly IServiceProvider _provider;

    public SerializerTests(IServiceProvider provider)
    {
        _provider = provider;
    }

    [Fact]
    public void CanSerializeASimpleType()
    {
        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new ConcreteConverterGenerator<BasicClass>(_provider));
        
        var json = JsonSerializer.Serialize(new BasicClass { SomeString = "One", SomeOtherString = "Two", BaseString = "Base"}, opts);
        
        json.Should().Contain("\"$type\":\"BasicClass\"", "data is serialized with a type hint");
        
        var data = JsonSerializer.Deserialize<BasicClass>(json, opts);
        
        data.SomeString.Should().Be("One", "SomeString should be deserialized");
        data.SomeOtherString.Should().Be(null, "SomeOtherString has a JsonIgnore attribute");
    }
    
    [Fact]
    public void CanSerializeAnAdvancedType()
    {
        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new ConcreteConverterGenerator<BasicClass>(_provider));
        opts.Converters.Add(new ConcreteConverterGenerator<AdvancedClass>(_provider));
        
        var originalData = new AdvancedClass()
        {
            SubClass = new BasicClass { SomeString = "Some", SomeOtherString = "String", BaseString = "Base2"},
            ListOfInts = new List<int> { 4, 2 }, 
            SomeInt = 42,
            BaseString = "Base"
        };
        
        var json = JsonSerializer.Serialize(originalData, opts);
        
        json.Should().Contain("\"$type\":\"ClassWithInts\"", "data is serialized with a type hint");
        json.Should().Contain("\"$type\":\"BasicClass\"", "sub data is serialized with a type hint");

        originalData.SubClass.SomeOtherString = null!;
        var data = JsonSerializer.Deserialize<AdvancedClass>(json, opts);

        data.Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void CanSerializeViaInterfaceTypes()
    {
        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new ConcreteConverterGenerator<BasicClass>(_provider));
        opts.Converters.Add(new ConcreteConverterGenerator<AdvancedClass>(_provider));
        opts.Converters.Add(new AbstractClassConverterGenerator<IInterface>(_provider));
        
        var json = JsonSerializer.Serialize<IInterface>(new BasicClass { SomeString = "One", SomeOtherString = "Two", BaseString = "Base"}, opts);
        
        json.Should().Contain("\"$type\":\"BasicClass\"", "data is serialized with a type hint");
        
        var data = (BasicClass)JsonSerializer.Deserialize<IInterface>(json, opts);
        
        data.SomeString.Should().Be("One", "SomeString should be deserialized");
        data.SomeOtherString.Should().Be(null, "SomeOtherString has a JsonIgnore attribute");
        
    }
    
    public interface IInterface
    {
        public string BaseString { get; init; }
    }

    public abstract class ABase : IInterface
    {
        public required string BaseString { get; init; }
    }

    [JsonName("BasicClass")]
    public class BasicClass : ABase
    {
        public string SomeString { get; set; }
        
        [JsonIgnore]
        public string SomeOtherString { get; set; }
    }
    
    [JsonName("ClassWithInts")]
    public class AdvancedClass : ABase
    {
        public int SomeInt { get; set; }
        public List<int> ListOfInts { get; set; }
        public BasicClass SubClass { get; set; }
    }
}
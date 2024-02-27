using System.Text.Json; 

namespace IOptionsWriter.IntegrationTests
{
    [TestFixture]
    public class OptionsWritableTests
    {
        private const string AppSettingsFile = "appsettings.json";
        private IHost TestHost = null!;

        [SetUp]
        public void Setup()
        {
            File.Delete(AppSettingsFile);
            //create test host
            this.TestHost = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddOptions().ConfigureWritable<TestOptions>();
                }).Build();
        }

        [TearDown]
        public void TearDown()
        {
            File.Delete(AppSettingsFile);
        }

        [Test]
        public async Task OptionsWriterShouldNotUpdateOtherSections()
        {
            var expectedSection = new { TestSection1 = new { Key1 = "Value1" } };

            var json =  JsonSerializer.SerializeToUtf8Bytes(expectedSection);
            //save test section to appsettings.json file
            await using (var stream = File.Create(AppSettingsFile))
            {
                stream.Write(json);
            }
            
            //update options
            var optionsWritable = this.TestHost.Services.GetRequiredService<IOptionsWritable<TestOptions>>();
            await optionsWritable.Update(options => options.TestOption = "some update event value",CancellationToken.None);
            
            //reread test section from file 
            var newJson = await File.ReadAllTextAsync(AppSettingsFile);
            var parsedObject = JsonDocument.Parse(newJson).RootElement.EnumerateObject()
                .SingleOrDefault(p => p.NameEquals(nameof(expectedSection.TestSection1)))
                .Value.EnumerateObject()
                .SingleOrDefault(p => p.NameEquals(nameof(expectedSection.TestSection1.Key1)))
                .Value.GetString(); ;
            Assert.That(parsedObject, Is.EqualTo(expectedSection.TestSection1.Key1));

        }

        [Test]
        public async Task OptionsWriterShouldRefreshValue()
        {
            var expectedTestOptionValue = "changedValue";

            var optionsWritable = this.TestHost.Services.GetRequiredService<IOptionsWritable<TestOptions>>();
            await optionsWritable.Update(options => options.TestOption = expectedTestOptionValue, CancellationToken.None);
            var anotherOptionsWritable = this.TestHost.Services.GetRequiredService<IOptionsWritable<TestOptions>>();
            Assert.That(anotherOptionsWritable.Value.TestOption, Is.EqualTo(expectedTestOptionValue));
        }

        [Test]
        public void OptionsWriterShouldReturnDefaultValue()
        {
            const string expectedTestOptionValue = "defaultOptions";

            var optionsWritable = this.TestHost.Services.GetRequiredService<IOptionsWritable<TestOptions>>();
            Assert.That(optionsWritable.Value.TestOption, Is.EqualTo(expectedTestOptionValue));
        }

        [Test]
        public async Task OptionsWriterShouldUpdateFileContent()
        {
            const string expectedTestOptionValue = "changedValue";

            var optionsWritable = this.TestHost.Services.GetRequiredService<IOptionsWritable<TestOptions>>();
            await optionsWritable.Update(options => options.TestOption = expectedTestOptionValue, CancellationToken.None);
            var parsedObject = JsonSerializer.Deserialize<TestOptions>(await File.ReadAllTextAsync(AppSettingsFile));

            Assert.That(parsedObject, Is.Not.Null);
            Assert.That(parsedObject?.TestOption, Is.EqualTo(expectedTestOptionValue));
        }
    }
}
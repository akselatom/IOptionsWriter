using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace IOptionsWriter;

public class OptionsWritable<T> : IOptionsWritable<T> where T : class, new()
{
    private readonly IConfigurationRoot _configurationRoot;
    private readonly IHostEnvironment _environment;
    private readonly bool _forceReloadAfterWrite;
    private readonly IOptionsMonitor<T> _options;
    private readonly string _section;
    private readonly string _settingsFile;

    public OptionsWritable(IHostEnvironment environment,
        IOptionsMonitor<T> options,
        IConfigurationRoot configurationRoot,
        string section,
        string settingsFile,
        bool forceReloadAfterWrite = false)
    {
        this._environment = environment;
        this._options = options;
        this._configurationRoot = configurationRoot;
        this._section = section;
        this._settingsFile = settingsFile;
        this._forceReloadAfterWrite = forceReloadAfterWrite;
    }

    public T Get(string name)
    {
        return this._options.Get(name);
    }

    public async Task Update(Action<T> applyChanges)
    {
        var fullPath = Path.IsPathRooted(this._settingsFile)
            ? this._settingsFile
            : this._environment.ContentRootFileProvider.GetFileInfo(this._settingsFile).PhysicalPath ?? this._settingsFile;

        applyChanges(this.Value);

        if (!File.Exists(fullPath))
        {
            var newJson = JsonSerializer.Serialize(this.Value);
            await File.WriteAllTextAsync(fullPath, newJson);
        }
        else
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, object>>(
                await File.ReadAllTextAsync(fullPath), 
                new JsonSerializerOptions()
                {
                    ReadCommentHandling = JsonCommentHandling.Skip
                });
            values[this._section] = this.Value;
            var updatedConfigJson =
                JsonSerializer.Serialize(values, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(fullPath, updatedConfigJson);
        }

        if (this._forceReloadAfterWrite) this._configurationRoot.Reload();
    }

    public T Value => this._options.CurrentValue;
}
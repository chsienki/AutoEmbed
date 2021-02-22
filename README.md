# AutoEmbed

Automatically add files and directory structures to your assembly, with easy generated access at runtime.

## Usage

Reference the generator, and add any files you want to embed to your project file via the `AutoEmbed` item. 

```xml
<ItemGroup>
    <AutoEmbed Include="files\**" />
</ItemGroup>
```

Usual MSBuild rules apply, so you may use wildcards, excludes etc.

The generate will add class called `Resources` to your compilation under the namespace `AutoEmbed` which contains properties allowing you to access the embedded resources at runtime.

```csharp
    string fileA = Resources.fileA_txt;
```

Files within subdirectories are included within sub-types:

```csharp
    string fileB = Resources.subdir.fileB_txt;
```

*Note:* nested directories with a single child directory are 'collapsed' into the parent folder.

Files are not limited to text only. The underlying type used to represent the resource contains implicit conversion for `string`, `byte[]` and `stream` meaning you can read a binary file either directly as an array or pass to a reader as a stream.

```csharp
byte[] fileC = Resources.subdir.fileC_bin;

using (var fileCStream = new BinaryReader(Resources.subdir.fileC_bin))
{
    Console.WriteLine(fileCStream.ReadInt32());
    Console.WriteLine(fileCStream.ReadBoolean());
    Console.WriteLine(fileCStream.ReadString());
}
```

The underlying objects also define `ReadAsString()`, `ReadAsByteArray()` and `ReadAsStream()` to allow you to access the data without relying on conversions

```csharp
var s = Resources.fileA_txt.ReadAsString();
```


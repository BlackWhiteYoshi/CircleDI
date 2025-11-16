# CircleDI.Blazor

The world only full-power circular Service Provider wired up with Blazor.

- Resolves Circular Dependencies
- Optimal Performance
- Compile Time Safety
- No Runtime Dependencies
- AOT friendly
- Beautiful Code Generation
- Non-Generic
- Non-Lazy Instantiation
- Object Oriented
- Customizable
- Easy to Use

Extra Blazor features are

- CircleDIComponentActivator.cs
- Automatic component registration as transient services.
- Default services of *Microsoft.Extensions.DependencyInjection* provider get added.

This package is not compatible with [CircleDI](https://www.nuget.org/packages/CircleDI) (this package already includes it).


## Requirements

- Language Version C#12 (default of .NET 8)
  - if you are using an older TargetFramework:
    - **.NET**
    => just set the language version to at least C#12.
    - **.NET Standard 2.1**
    => together with the LangVersion requirement you also need some polyfills, I recommend using [PolySharp](https://github.com/Sergio0694/PolySharp).
    - **.NET Framework, UWP, .NET Standard 2.0**
    => LangVersion C#12 or newer, polyfills ([PolySharp](https://github.com/Sergio0694/PolySharp)) and disable DisposeAsync generation.


For documentation or sourcecode see [github.com/BlackWhiteYoshi/CircleDI](https://github.com/BlackWhiteYoshi/CircleDI).

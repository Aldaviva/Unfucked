// Sadly, this conflicts with any other package that declares this class, such as System.Text.Json for net462. This causes build failures when using ILRepack, and requires you to internalize types to allow ILRepack to deduplicate these classes.
// If ILRepack complains about this class being a duplicate, see https://github.com/Aldaviva/Fail2Ban4Win/blob/91709d6a666f5f8977cb39c324bc5453ee5f3eec/Fail2Ban4Win/ILRepack.targets#L13

// #if !NET5_0_OR_GREATER

using System.ComponentModel;

// ReSharper disable once CheckNamespace - this is the exact namespace required by this type to fix broken record compilation for .NET Standard 2.0
namespace System.Runtime.CompilerServices;

/// <summary>
/// <para>Needed to make .NET Standard 2.0 stop breaking the build when a record type with auto-initialized property parameters is defined.</para>
/// <para>From <see href="https://stackoverflow.com/a/62656145/979493"/></para>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal class IsExternalInit;
// #endif
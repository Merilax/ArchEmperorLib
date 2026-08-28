using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

#if NET6_0_OR_GREATER
// Implements nullables
namespace System.Runtime.CompilerServices
{
	internal static class IsExternalInit { }

	[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
	internal sealed class NullableAttribute : Attribute
	{
		public readonly byte[] NullableFlags;
		public NullableAttribute(byte flag) => NullableFlags = new[] { flag };
		public NullableAttribute(byte[] flags) => NullableFlags = flags;
	}

	[AttributeUsage(AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
	internal sealed class NullableContextAttribute : Attribute
	{
		public readonly byte Flag;
		public NullableContextAttribute(byte flag) => Flag = flag;
	}
}
#endif

namespace ArchEmperorLib
{
	/// <summary>How strictly a version must match between two peers.</summary>
	public enum VersionStrictness { None, Major, Minor, Exact }

	/// <summary>Where a mod (or bundle) is required to be present.</summary>
	public enum RequirementScope { Everyone, HostOnly, ClientOptional }

	public readonly struct ModRecord(string id, string name, string version, RequirementScope scope, VersionStrictness strictness)
	{
		public string Id { get; } = id;
		public string Name { get; } = name;
		public string Version { get; } = version;
		public RequirementScope Scope { get; } = scope;
		public VersionStrictness Strictness { get; } = strictness;
	}

	/// <summary>A single sub-item a mod wants matched, e.g. an asset/content bundle.</summary>
	public readonly struct BundleRecord(string name, string version)
	{
		public string Name { get; } = name;
		public string Version { get; } = version;
	}

	public static class ModRegistry
	{
		private static readonly Dictionary<string, ModRecord> _records = new();
		private static readonly Dictionary<string, Func<IReadOnlyList<BundleRecord>>> _bundleProviders = new();
		// private static readonly Dictionary<string, ICustomCompatibilityCheck> _customChecks = new();

		public static void Register(
			string id,
			string name,
			string version,
			RequirementScope scope = RequirementScope.Everyone,
			VersionStrictness strictness = VersionStrictness.Exact,
			// ICustomCompatibilityCheck customCheck = null,
			Func<IReadOnlyList<BundleRecord>> bundles = null)
		{
			_records[id] = new ModRecord(id, name, version, scope, strictness);

			// if (customCheck != null) _customChecks[id] = customCheck; TODO
			if (bundles != null) _bundleProviders[id] = bundles;
		}

		public static IReadOnlyCollection<ModRecord> LocalMods => _records.Values;

		public static Func<IReadOnlyList<BundleRecord>> GetBundles(string id)
		{
			if (_bundleProviders.ContainsKey(id))
				return _bundleProviders[id];
			return null;
		}

		// Quickly compute a hash of the local mod and bundle set.
		public static string ComputeDigest(bool doNotHash = false)
		{
			// Add the basic data of each mod.
			var parts = new List<string>();
			foreach (var modRec in _records.Values.OrderBy(r => r.Id, StringComparer.Ordinal))
			{
				// Do not pre-filter mods that won't affect clients.
				if (modRec.Scope != RequirementScope.Everyone) continue;

				parts.Add($"{modRec.Id}@{modRec.Version}:{modRec.Scope}:{modRec.Strictness}");
				Plugin.LogDebug($"{modRec.Id}@{modRec.Version}:{modRec.Scope}:{modRec.Strictness}");

				if (_bundleProviders.TryGetValue(modRec.Id, out var provider))
				{
					// If the mod has any bundles, add all of them, as they are obligatory.
					foreach (var bundleRec in provider().OrderBy(b => b.Name, StringComparer.Ordinal))
					{
						parts.Add($">{modRec.Id}/{bundleRec.Name}@{bundleRec.Version}");
						Plugin.LogDebug($">{modRec.Id}/{bundleRec.Name}@{bundleRec.Version}");
					}
				}
			}
			if (doNotHash) return string.Join("|", parts);
			using var sha = SHA256.Create();
			var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("|", parts)));

			Plugin.LogDebug(Convert.ToBase64String(hash));
			return Convert.ToBase64String(hash);
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SOTreeTool
{
    /// <summary>
    /// Scans loaded assemblies for ScriptableObject subclasses.
    /// GetProjectScriptableObjectTypes() returns only types from Assembly-CSharp
    /// (the user's own project code), skipping all Unity engine and package types.
    /// </summary>
    public static class SOTypeScanner
    {
        // ── Cached results ────────────────────────────────────────────────────
        private static List<string> _cachedAll     = null;
        private static List<string> _cachedProject = null;
        private static List<Type>   _cachedAllObjs = null;

        // ── Project-only (Assembly-CSharp) ────────────────────────────────────
        /// <summary>Returns type names from the project's own Assembly-CSharp only.</summary>
        public static List<string> GetProjectScriptableObjectTypes(bool forceRefresh = false)
        {
            if (_cachedProject != null && !forceRefresh)
                return _cachedProject;

            _cachedProject = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name == "Assembly-CSharp")
                .SelectMany(SafeGetTypes)
                .Where(IsProjectSO)
                .Select(t => t.Name)
                .OrderBy(n => n)
                .ToList();

            return _cachedProject;
        }

        // ── All assemblies (for advanced use) ─────────────────────────────────
        /// <summary>Returns type names from every loaded assembly.</summary>
        public static List<string> GetAllScriptableObjectTypes(bool forceRefresh = false)
        {
            if (_cachedAll != null && !forceRefresh)
                return _cachedAll;

            _cachedAll = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .Where(IsProjectSO)
                .Select(t => t.Name)
                .OrderBy(n => n)
                .ToList();

            return _cachedAll;
        }

        /// <summary>Returns full Type objects for every project SO.</summary>
        public static List<Type> GetAllScriptableObjectTypeObjects(bool forceRefresh = false)
        {
            if (_cachedAllObjs != null && !forceRefresh)
                return _cachedAllObjs;

            _cachedAllObjs = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .Where(IsProjectSO)
                .OrderBy(t => t.Name)
                .ToList();

            return _cachedAllObjs;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static bool IsProjectSO(Type t) =>
            t.IsClass &&
            !t.IsAbstract &&
            typeof(ScriptableObject).IsAssignableFrom(t) &&
            t != typeof(ScriptableObject);

        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try   { return assembly.GetTypes(); }
            catch { return Enumerable.Empty<Type>(); }
        }

        /// <summary>Clears all caches so the next call rescans.</summary>
        public static void ClearCache()
        {
            _cachedAll     = null;
            _cachedProject = null;
            _cachedAllObjs = null;
        }
    }
}

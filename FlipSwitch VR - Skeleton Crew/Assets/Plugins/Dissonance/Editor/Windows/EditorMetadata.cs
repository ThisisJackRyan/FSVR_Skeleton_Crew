using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Dissonance.Editor.Windows.Welcome;
using Dissonance.Extensions;
using UnityEngine;

namespace Dissonance.Editor.Windows
{
    /// <summary>
    /// Get some anonymised metadata about the current editor
    /// </summary>
    internal class EditorMetadata
    {
        /// <summary>
        /// Get an anonymous unique ID for this user
        /// </summary>
        /// <returns></returns>
        [NotNull] private static string UserId()
        {
            var parts = string.Format("{0}-{1}", Environment.MachineName, Environment.UserName);

            using (var sha512 = SHA1.Create())
                return Convert.ToBase64String(sha512.ComputeHash(Encoding.UTF8.GetBytes(parts))).Replace("+", "").Replace("/", "");
        }

        /// <summary>
        /// Get the version of the editor
        /// </summary>
        /// <returns></returns>
        private static string UnityVersion()
        {
            return WWW.EscapeURL(Application.unityVersion);
        }

        [NotNull] internal static string GetQueryString([CanBeNull] IEnumerable<KeyValuePair<string, string>> parts = null)
        {
            var extensions = (parts ?? new KeyValuePair<string, string>[0])
                .Select(a => string.Format("{0}={1}", a.Key, a.Value))
                .Concat(string.Format("uid={0}", UserId()))
                .Concat(string.Format("uve={0}", UnityVersion()))
                .Concat(string.Format("dve={0}", WelcomeLauncher.CurrentDissonanceVersion));

            return "?" + string.Join("&", extensions.ToArray());
        }
    }
}

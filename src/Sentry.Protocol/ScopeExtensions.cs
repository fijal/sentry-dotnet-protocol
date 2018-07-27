using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Sentry.Protocol;

// ReSharper disable once CheckNamespace
namespace Sentry
{
    ///
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ScopeExtensions
    {
        /// <summary>
        /// Adds a breadcrumb to the scope
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="message">The message.</param>
        /// <param name="type">The type.</param>
        /// <param name="category">The category.</param>
        /// <param name="dataPair">The data key-value pair.</param>
        /// <param name="level">The level.</param>
        public static void AddBreadcrumb(
                    this Scope scope,
                    string message,
                    string category,
                    string type,
                    Tuple<string, string> dataPair = null,
                    BreadcrumbLevel level = 0)
        {
            Dictionary<string, string> data = null;
            if (dataPair != null)
            {
                data = new Dictionary<string, string>
                {
                    {dataPair.Item1, dataPair.Item2}
                };
            }

            scope.AddBreadcrumb(
                timestamp: null,
                message: message,
                category: category,
                type: type,
                data: data,
                level: level);
        }

        /// <summary>
        /// Adds a breadcrumb to the scope.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="type">The type.</param>
        /// <param name="data">The data.</param>
        /// <param name="level">The level.</param>
        public static void AddBreadcrumb(
                    this Scope scope,
                    string message,
                    string category = null,
                    string type = null,
                    Dictionary<string, string> data = null,
                    BreadcrumbLevel level = 0)
        {
            scope.AddBreadcrumb(null, message, category, type, null, level);
        }

        /// <summary>
        /// Adds a breadcrumb to the scope
        /// </summary>
        /// <remarks>
        /// This overload is used for testing.
        /// </remarks>
        /// <param name="scope">The scope.</param>
        /// <param name="timestamp">The timestamp</param>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="type">The type.</param>
        /// <param name="data">The data</param>
        /// <param name="level">The level.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddBreadcrumb(this Scope scope,
            DateTimeOffset? timestamp,
            string message,
            string category = null,
            string type = null,
            IReadOnlyDictionary<string, string> data = null,
            BreadcrumbLevel level = 0)
        {
            scope.AddBreadcrumb(new Breadcrumb(
                timestamp: timestamp,
                message: message,
                type: type,
                data: data,
                category: category,
                level: level));
        }

        /// <summary>
        /// Adds a breadcrumb to the <see cref="Scope"/>
        /// </summary>
        /// <param name="scope">Scope</param>
        /// <param name="breadcrumb">The breadcrumb.</param>
        internal static void AddBreadcrumb(this Scope scope, Breadcrumb breadcrumb)
        {
            var breadcrumbs = (ConcurrentQueue<Breadcrumb>)scope.Breadcrumbs;

            var overflow = breadcrumbs.Count - (scope.Options?.MaxBreadcrumbs
                                                ?? Constants.DefaultMaxBreadcrumbs) + 1;
            if (overflow > 0)
            {
                Breadcrumb result;
                breadcrumbs.TryDequeue(out result);
            }

            breadcrumbs.Enqueue(breadcrumb);
        }

        /// <summary>
        /// Sets the fingerprint to the <see cref="Scope"/>
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="fingerprint">The fingerprint.</param>
        public static void SetFingerprint(this Scope scope, IEnumerable<string> fingerprint)
            => scope.InternalFingerprint = fingerprint;

        /// <summary>
        /// Sets the extra key-value to the <see cref="Scope"/>
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void SetExtra(this Scope scope, string key, object value)
            => ((ConcurrentDictionary<string, object>)scope.Extra).AddOrUpdate(key, value, (s, o) => value);

        /// <summary>
        /// Sets the extra key-value pairs to the <see cref="Scope"/>
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="values">The values.</param>
        public static void SetExtras(this Scope scope, IEnumerable<KeyValuePair<string, object>> values)
        {
            var extra = (ConcurrentDictionary<string, object>)scope.Extra;
            foreach (var keyValuePair in values)
            {
                extra.AddOrUpdate(keyValuePair.Key, keyValuePair.Value, (s, o) => keyValuePair.Value);
            }
        }

        /// <summary>
        /// Sets the tag to the <see cref="Scope"/>
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void SetTag(this Scope scope, string key, string value)
            => ((ConcurrentDictionary<string, string>)scope.Tags).AddOrUpdate(key, value, (s, o) => value);

        /// <summary>
        /// Set all items as tags
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="tags"></param>
        public static void SetTags(this Scope scope, IEnumerable<KeyValuePair<string, string>> tags)
        {
            var internalTags = (ConcurrentDictionary<string, string>)scope.Tags;
            foreach (var keyValuePair in tags)
            {
                internalTags.AddOrUpdate(keyValuePair.Key, keyValuePair.Value, (s, o) => keyValuePair.Value);
            }
        }

        /// <summary>
        /// Removes a tag from the <see cref="Scope"/>
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="key"></param>
        public static void UnsetTag(this Scope scope, string key)
        {
            string result;
            scope.InternalTags?.TryRemove(key, out result);
        }
            
        /// <summary>
        /// Applies the data from one scope to the other while
        /// </summary>
        /// <param name="from">The scope to data copy from.</param>
        /// <param name="to">The scope to copy data to.</param>
        /// <remarks>
        /// Applies the data of 'from' into 'to'.
        /// If data in 'from' is null, 'to' is unmodified.
        /// Conflicting keys are not overriden
        /// This is a shallow copy.
        /// </remarks>
        public static void Apply(this Scope from, Scope to)
        {
            if (from == null || to == null)
            {
                return;
            }

            // Fingerprint isn't combined. It's absolute.
            // One set explicitly on target (i.e: event)
            // takes precedence and is not overwritten
            if (to.InternalFingerprint == null
                && from.InternalFingerprint != null)
            {
                to.InternalFingerprint = from.InternalFingerprint;
            }

            if (from.InternalBreadcrumbs != null)
            {
                ((ConcurrentQueue<Breadcrumb>)to.Breadcrumbs).EnqueueAll(from.InternalBreadcrumbs);
            }

            if (from.InternalExtra != null)
            {
                foreach (var extra in from.Extra)
                {
                    ((ConcurrentDictionary<string, object>)to.Extra).TryAdd(extra.Key, extra.Value);
                }
            }

            if (from.InternalTags != null)
            {
                foreach (var tag in from.Tags)
                {
                    ((ConcurrentDictionary<string, string>)to.Tags).TryAdd(tag.Key, tag.Value);
                }
            }

            from.InternalContexts?.CopyTo(to.Contexts);
            from.InternalRequest?.CopyTo(to.Request);
            from.InternalUser?.CopyTo(to.User);

            if (to.Environment == null)
            {
                to.Environment = from.Environment;
            }

            if (from.Sdk != null)
            {
                if (from.Sdk.Name != null && from.Sdk.Version != null)
                {
                    to.Sdk.Name = from.Sdk.Name;
                    to.Sdk.Version = from.Sdk.Version;
                }

                if (from.Sdk.InternalIntegrations != null)
                {
                    foreach (var integration in from.Sdk.Integrations)
                    {
                        to.Sdk.AddIntegration(integration);
                    }
                }
            }
        }

        /// <summary>
        /// Applies the state object into the scope
        /// </summary>
        /// <param name="scope">The scope to apply the data.</param>
        /// <param name="state">The state object to apply.</param>
        public static void Apply(this Scope scope, object state)
        {
            if (state is string)
            {
                var scopeString = (string)state;
                // TODO: find unique key to support multiple single-string scopes
                scope.SetTag("scope", scopeString);
            }
            else if (state is IEnumerable<KeyValuePair<string, string>>)
            {
                var keyValStringString = (IEnumerable<KeyValuePair<string, string>>)state;
                scope.SetTags(keyValStringString
                        .Where(kv => !string.IsNullOrEmpty(kv.Value)));
            }
            else if (state is IEnumerable<KeyValuePair<string, object>>)
            {
                var keyValStringObject = (IEnumerable<KeyValuePair<string, object>>)state;
                scope.SetTags(keyValStringObject
                    .Select(k => new KeyValuePair<string, string>(
                        k.Key,
                        k.Value?.ToString()))
                    .Where(kv => !string.IsNullOrEmpty(kv.Value)));

            }
            else if (state is ValueTuple<string, string>)
            {
                var tupleStringString = (ValueTuple<string, string>)state;
                if (!string.IsNullOrEmpty(tupleStringString.Item2))
                {
                    scope.SetTag(tupleStringString.Item1, tupleStringString.Item2);
                }
            }
            else
            {
                scope.SetExtra("state", state);
            }
        }
    }
}

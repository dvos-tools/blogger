using System;
using UnityEngine;

namespace com.DvosTools.blogger.Context
{
    /// <summary>
    /// Global context for logging metadata. Set once, automatically included in all logs.
    /// </summary>
    public static class LoggingContext
    {
        private static string _userId = "anonymous";
        private static string _sessionId;
        private static string _environment = "development";
        
        /// <summary>
        /// Current user ID. Set this when user logs in.
        /// Example: LoggingContext.SetUserId("user_12345");
        /// </summary>
        public static string UserId => _userId;
        
        /// <summary>
        /// Current session ID. Auto-generated on app start.
        /// </summary>
        public static string SessionId => _sessionId;
        
        /// <summary>
        /// Current environment (development, staging, production).
        /// </summary>
        public static string Environment => _environment;
        
        /// <summary>
        /// Current Unity scene name. Dynamically retrieved.
        /// </summary>
        public static string CurrentScene => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        /// <summary>
        /// Platform (iOS, Android, Windows, etc.). Dynamically retrieved.
        /// </summary>
        public static string Platform => Application.platform.ToString();
        
        /// <summary>
        /// App version.
        /// </summary>
        public static string AppVersion => Application.version;
        
        /// <summary>
        /// Unity version.
        /// </summary>
        public static string UnityVersion => Application.unityVersion;

        // Initialize session on first access
        static LoggingContext()
        {
            _sessionId = Guid.NewGuid().ToString("N").Substring(0, 16);
        }

        /// <summary>
        /// Set the current user ID. Call this after user login.
        /// </summary>
        /// <param name="userId">User identifier (email, username, UUID, etc.)</param>
        public static void SetUserId(string userId)
        {
            _userId = string.IsNullOrEmpty(userId) ? "anonymous" : userId;
            Debug.Log($"[LoggingContext] UserId set to: {_userId}");
        }

        /// <summary>
        /// Clear user ID on logout.
        /// </summary>
        public static void ClearUserId()
        {
            _userId = "anonymous";
            Debug.Log("[LoggingContext] UserId cleared (set to anonymous)");
        }

        /// <summary>
        /// Set the environment (development, staging, production).
        /// </summary>
        /// <param name="environment">Environment name</param>
        public static void SetEnvironment(string environment)
        {
            _environment = environment;
            Debug.Log($"[LoggingContext] Environment set to: {_environment}");
        }

        /// <summary>
        /// Set a custom session ID. Use this when the session ID is controlled externally (e.g., by a server).
        /// </summary>
        /// <param name="sessionId">Session identifier from external source (server, analytics, etc.)</param>
        public static void SetSessionId(string sessionId)
        {
            _sessionId = string.IsNullOrEmpty(sessionId) ? Guid.NewGuid().ToString("N").Substring(0, 16) : sessionId;
            Debug.Log($"[LoggingContext] SessionId set to: {_sessionId}");
        }

        /// <summary>
        /// Generate a new session ID. Useful for tracking separate play sessions.
        /// </summary>
        public static void ResetSession()
        {
            _sessionId = Guid.NewGuid().ToString("N").Substring(0, 16);
            Debug.Log($"[LoggingContext] New session started: {_sessionId}");
        }

        /// <summary>
        /// Get all context as a formatted string for logging.
        /// </summary>
        public static string GetFormattedContext()
        {
            return $"userId={_userId} sessionId={_sessionId} env={_environment} scene={CurrentScene} platform={Platform} version={AppVersion}";
        }
    }
}

﻿/*
 * 
 * Author: Eric Ng Jun Feng
 * 
 */
namespace Syncless.Notification
{
    /// <summary>
    /// IQueueObserver is an interface which provides a mechanism for receiving notifications from classes
    /// implementing <see cref="INotificationQueue">INotificationQueue</see> interface.
    /// </summary>
    public interface IQueueObserver
    {
        /// <summary>
        /// Notifies the observer that modification has occurred in the notification queue
        /// </summary>
        void Update();
    }
}

﻿namespace AngleSharp
{
    using AngleSharp.DOM;
    using AngleSharp.Extensions;
    using AngleSharp.Infrastructure;
    using AngleSharp.Services;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Couples the mutation events to mutation observers and the event loop.
    /// </summary>
    sealed class MutationHost
    {
        #region Fields

        readonly List<MutationObserver> _observers;
        readonly Document _document;
        Boolean _queued;

        #endregion

        #region ctor

        public MutationHost(Document document)
        {
            _observers = new List<MutationObserver>();
            _queued = false;
            _document = document;
        }

        #endregion

        #region Properties

        public IEnumerable<MutationObserver> Observers
        {
            get { return _observers; }
        }

        #endregion

        #region Methods

        public void Register(MutationObserver observer)
        {
            if (_observers.Contains(observer) == false)
                _observers.Add(observer);
        }

        public void Unregister(MutationObserver observer)
        {
            if (_observers.Contains(observer) == true)
                _observers.Remove(observer);
        }

        /// <summary>
        /// Enqueues the flushing of the mutation observers in the event loop.
        /// </summary>
        public void ScheduleCallback()
        {
            if (_queued)
                return;

            var context = _document.Context;

            if (context == null)
                return;

            var eventLoop = context.Configuration.GetService<IEventService>();

            if (eventLoop == null)
                return;

            _queued = true;
            Func<Task> task = DispatchCallback;
            eventLoop.Enqueue(new MicroDomTask(_document, task));
        }

        /// <summary>
        /// Notifies the registered observers with all registered changes.
        /// </summary>
        /// <returns>The awaitable task.</returns>
        public async Task DispatchCallback()
        {
            var notifyList = _observers.ToArray();
            var context = _document.Context;

            if (context == null)
                return;

            var eventLoop = context.Configuration.GetService<IEventService>();

            if (eventLoop == null)
                return;

            _queued = false;

            foreach (var mo in notifyList)
            {
                await eventLoop.Execute(() => mo.Trigger());
            }
        }

        #endregion
    }
}

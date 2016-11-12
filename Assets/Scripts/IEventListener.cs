using UnityEngine;
using System.Collections;

namespace StageNine.Events
{
    public enum EventType
    {
        NULL,
        RESIZE_SCREEN,
        UI_ECONOMY_CHECK
    }

    public interface IEventListener
    {
        // If you want an IEventListener to respond, it should check the eType
        // and respond appropriately.
        void HandleEvent(EventType eType);

        // These functions should connect to the EventManager singleton
        // They should be run in Start rather than Awake so they're run later
        bool Connect();
        bool Disconnect();
    }

}
namespace Core
{
    public struct OnReCalculateDepthEvent : IEvent
    {
        
    }
    public struct OnLevelFailedEvent : IEvent
    {
        
    }
    public struct OnBoxMovedFromBoxAreaEvent : IEvent
    {
        public Box Box;
    }
    public struct OnBoxFilledEvent : IEvent
    {
        
    }
    public struct OnBoxEndMoveEvent : IEvent
    {
        public Box Box;
    }
    
    public class Events
    {
    }
}
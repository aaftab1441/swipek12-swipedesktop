using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwipeDesktop.Common
{
    public class GenericEvent<T> : ISwipeEvent
    {
        public GenericEvent() { }
        public GenericEvent(T @event) {
            Payload = @event;
            Type = @event.GetType();
          
        }
        public T Payload {get;}
        
        public Type Type { get; set; }

        public long Id { get; set; }
    }

    public interface ISwipeEvent
    {
        Type Type { get; set; }
        long Id { get; set; }
        
    }
}

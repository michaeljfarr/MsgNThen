using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Features;

namespace MsgNThen.Adapter
{
    public class ItemsFeature : IItemsFeature
    {
        public ItemsFeature()
        {
            this.Items = new Dictionary<object, object>();
        }

        public IDictionary<object, object> Items { get; set; }
    }
}
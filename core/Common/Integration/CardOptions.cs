﻿using System;
namespace OnlineMarket.Core.Common.Integration
{
    public class CardOptions
    {
        public string Number { get; set; } = "";
        public string ExpMonth { get; set; } = "";
        public string ExpYear { get; set; } = "";
        public string Cvc { get; set; } = "";
    }
}


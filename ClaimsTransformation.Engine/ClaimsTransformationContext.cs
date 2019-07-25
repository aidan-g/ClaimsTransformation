﻿using System.Collections.Generic;
using System.Security.Claims;

namespace ClaimsTransformation.Engine
{
    public class ClaimsTransformationContext : IClaimsTransformationContext
    {
        private ClaimsTransformationContext()
        {
            this.Output = new List<Claim>();
        }

        public ClaimsTransformationContext(IEnumerable<Claim> input) : this()
        {
            this.Input = input;
        }

        public IEnumerable<Claim> Input { get; set; }

        public IEnumerable<Claim> Output { get; set; }
    }
}

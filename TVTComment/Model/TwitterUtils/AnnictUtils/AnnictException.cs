using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.TwitterUtils.AnnictUtils
{
    class AnnictException : InvalidOperationException
    {
        public AnnictException()
        {
        }

        public AnnictException(string message) : base(message)
        {
        }

        public AnnictException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    class AnnictResponseException : AnnictException
    {
        public AnnictResponseException()
        {
        }

        public AnnictResponseException(string message) : base(message)
        {
        }

        public AnnictResponseException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

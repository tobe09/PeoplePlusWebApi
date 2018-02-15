using System;

namespace PeoplePlusWebApi
{
    public class Values
    {
        public static string RootAddress
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; } //@"C:\Users\NeptuneDev1\Documents\Visual Studio 2015\Projects\PeoplePlusWebApi\PeoplePlusWebApi\"; }
        }
        
        public static string HostRootAddress
        {
            get { return RootAddress; }
        }

        public static string ErroMsg
        {
            get { return "An Error/Exception Has Occurred"; }
        }
    }
}
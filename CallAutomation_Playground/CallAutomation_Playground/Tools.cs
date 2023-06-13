using System.Text.RegularExpressions;

namespace CallAutomation_Playground
{
    public static class Tools
    {
        public static string FormatPhoneNumbers(string phoneNumber)
        {
            // calculate E.164 format phonenumber.
            // +1 xxx-xxx-xxxx
            // update this tools as your need.
            if (phoneNumber == null)
            {
                throw new ArgumentNullException(nameof(phoneNumber));
            }

            // Remove all non-digit characters from the phone number
            phoneNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());

            if (phoneNumber.Length == 10)
            {
                return "+1" + phoneNumber;
            }
            else if (phoneNumber.Length == 11 && phoneNumber.StartsWith("1"))
            {
                return "+" + phoneNumber;
            }
            else if (phoneNumber.Length == 12 && phoneNumber.StartsWith("+1"))
            {
                return phoneNumber;
            }
            else if (phoneNumber.Length == 12 && phoneNumber.StartsWith("91"))
            {
                return "+" + phoneNumber;
            }
            else
            {
                throw new ArgumentException("Invalid phone number");
            }
        }
        public enum CommunicationIdentifierKind
        {
            PhoneIdentity,
            UserIdentity,
            UnknownIdentity
        }
        public class Constants
        {
            public const string userIdentityRegex = @"8:acs:[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}_[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}";
            public const string phoneIdentityRegex = @"^\+\d{10,14}$";

        }
        public static CommunicationIdentifierKind GetIdentifierKind(string participantnumber)
        {
            //checks the identity type returns as string
            return Regex.Match(participantnumber, Constants.userIdentityRegex, RegexOptions.IgnoreCase).Success ? CommunicationIdentifierKind.UserIdentity :
                  Regex.Match(participantnumber, Constants.phoneIdentityRegex, RegexOptions.IgnoreCase).Success ? CommunicationIdentifierKind.PhoneIdentity :
                  CommunicationIdentifierKind.UnknownIdentity;
        }

    }
}

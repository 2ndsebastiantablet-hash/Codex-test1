namespace VRTemplate.Networking
{
    public readonly struct SessionInfo
    {
        public SessionInfo(string roomName, bool isPrivate, string privateCode)
        {
            RoomName = roomName;
            IsPrivate = isPrivate;
            PrivateCode = privateCode;
        }

        public string RoomName { get; }
        public bool IsPrivate { get; }
        public string PrivateCode { get; }

        public string ToDisplayString()
        {
            if (IsPrivate)
            {
                return $"Server: {RoomName} (Private) | Code: {PrivateCode}";
            }

            return $"Server: {RoomName} (Public)";
        }
    }
}

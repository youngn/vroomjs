namespace VroomJs
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="errorInfo"></param>
    /// <returns>True to proceed with raising the error in the script; false to prevent it.</returns>
    public delegate bool HostErrorFilterDelegate(IHostObjectCallbackContext context, HostErrorInfo errorInfo);
}

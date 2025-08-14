namespace ReverseMarket.Services
{
    public interface IWhatsAppService
    {
        Task<bool> SendMessageAsync(string phoneNumber, string message);
        Task<bool> SendOTPAsync(string phoneNumber, string otp);
        Task<bool> NotifyStoreAsync(string phoneNumber, string storeName, string requestTitle, string requestUrl);
        Task<bool> NotifyUserApprovalAsync(string phoneNumber, string userName, string requestTitle);
    }
}
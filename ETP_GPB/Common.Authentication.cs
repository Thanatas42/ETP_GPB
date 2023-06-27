using System;

namespace ETPlibrary.ETPGPB.Common.Authentication
{
    /// <summary>
    /// Провайдер для доступа к текущему токену авторизации для сервиса обмена.
    /// </summary>
    public interface IAuthTokenProvider
     {
         /// <summary>
        /// Токен аутентификации. Представлен в виде base64-строки.
         /// </summary>
        string AuthenticationToken { get; set; }
    }


    /// <summary>
    /// Аргументы события на истечение срока действия токена авторизации.
    /// </summary>
    public class TokenExpiredEventArgs : EventArgs
    {
        /// <summary>
        /// Признак того, что токен был обновлен.
        /// </summary>
        public bool TokenUpdated { get; set; }
    }


}

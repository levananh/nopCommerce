using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Security.Captcha
{
    /// <summary>
    /// HTML extensions
    /// </summary>
    public static class HtmlExtensions
    {
        /// <summary>
        /// Generate reCAPTCHA Control
        /// </summary>
        /// <param name="helper">HTML helper</param>
        /// <returns>Result</returns>
        public static GRecaptchaControl GenerateCaptcha(this IHtmlHelper helper)
        {
            var captchaSettings = EngineContext.Current.Resolve<CaptchaSettings>();
            var workContext = EngineContext.Current.Resolve<IWorkContext>();

            var culture = workContext.WorkingLanguage?.LanguageCulture.ToLower();
            var seoCode = workContext.WorkingLanguage?.UniqueSeoCode.ToLower();
            
            //this list got from this site: https://developers.google.com/recaptcha/docs/language
            var supportedLanguageCodes = new List<string> { "af", "am", "ar", "az", "bg", "bn", "ca", "cs", "da", "de", "de-AT", "de-CH", "el", "en", "en-GB", "es", "es-419", "et", "eu", "fa", "fi", "fil", "fr", "fr-CA", "gl", "gu", "hi", "hr", "hu", "hy", "id", "is", "it", "iw", "ja", "ka", "kn", "ko", "lo", "lt", "lv", "ml", "mn", "mr", "ms", "nl", "no", "pl", "pt", "pt-BR", "pt-PT", "ro", "ru", "si", "sk", "sl", "sr", "sv", "sw", "ta", "te", "th", "tr", "uk", "ur", "Value", "Value", "vi", "zh-CN", "zh-HK", "zh-TW", "zu" };

            var lang = supportedLanguageCodes.Contains(culture) ? culture :
                supportedLanguageCodes.Contains(seoCode) ? seoCode : captchaSettings.ReCaptchaDefaultLanguage;

            //generate captcha control
            var captchaControl = new GRecaptchaControl
            {
                Theme = captchaSettings.ReCaptchaTheme,
                Id = "recaptcha",
                PublicKey = captchaSettings.ReCaptchaPublicKey,
                Language =  lang
            };

            return captchaControl;
        }
    }
}
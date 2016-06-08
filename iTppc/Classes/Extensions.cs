using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace iTppc.Classes
{
    public static class Extensions
    {
        public static void safeInvoke(this Control c, Action callback)
        {
            if (c.InvokeRequired)
            {
                c.Invoke(new MethodInvoker(() =>
                {
                    callback();
                }));
            }
            else
            {
                callback();
            }
        }

        public static void safeBeginInvoke(this Control c, Action callback)
        {
            if (c.InvokeRequired)
            {
                c.BeginInvoke(new MethodInvoker(() =>
                {
                    callback();
                }));
            }
            else
            {
                callback();
            }
        }

        public static void resizelocate(this PictureBox p)
        {
            p.safeInvoke(() =>
            {
                if (p.Image.Width > p.Width || p.Image.Height > p.Height)
                {
                    double ratio = (double)p.Image.Width / (double)p.Image.Height;
                    int mid = p.Location.X + (int)Math.Floor((double)p.Width / 2);
                    int newWidth = (int)Math.Round(ratio * p.Width);

                    p.Width = newWidth;
                    p.Location = new Point(mid - (int)Math.Floor((double)newWidth / 2), p.Location.Y);
                }
                else
                {
                    p.SizeMode = PictureBoxSizeMode.CenterImage;
                }
            });
        }

        public static double getDouble(this TextBox t, double def)
        {
            double ret = def;

            t.safeInvoke(() =>
            {
                Double.TryParse(t.Text, out ret);
            });

            return ret;
        }

        public static int getInt(this TextBox t, int def)
        {
            int ret = def;

            t.safeInvoke(() =>
            {
                Int32.TryParse(t.Text, out ret);
            });

            return ret;
        }

        public static string ToDecimalString(this double d)
        {
            return d % 1 == 0 ? d.ToString("f1") : d.ToString();
        }
    }
}

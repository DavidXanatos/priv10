using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateWin10.Pages
{
    public interface IUserPage
    {
        void OnShow();

        void OnHide();

        void OnClose();
    }
}

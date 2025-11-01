using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


class Solution
{
    /**
     * 代码中的类名、方法名、参数名已经指定，请勿修改，直接返回方法规定的值即可
     *
     *
     * @param username string字符串 用户名
     * @param password string字符串 密码
     * @return bool布尔型
     */
    public bool SignUp(string username, string password)
    {
        // Check username
        return CheckUsername(username) && CheckPassword(password, username);
    }

    private enum ValidPasswordType
    {
        UpperCase = 0,
        LowerCase = 1,
        Digit = 2,
        SpecialChar = 3
    }

    private bool CheckPassword(string passWordOrigin,
       string userNameOri)
    {
        var password = passWordOrigin.AsSpan();
        var userName = userNameOri.AsSpan();
        if (password.Length is < 8 or > 16) return false;
        var typeArray = new bool[4];
        var specialChars = new HashSet<char>()
        {
           '~','!','@','#','$','%'
        };
        const char point = '.';
        foreach (var value in password)
        {
            if (char.IsUpper(value))
                typeArray[(int)ValidPasswordType.UpperCase] = true;
            else if (char.IsLower(value))
                typeArray[(int)ValidPasswordType.LowerCase] = true;
            else if (char.IsDigit(value))
                typeArray[(int)ValidPasswordType.Digit] = true;
            else if (specialChars.Contains(value))
                typeArray[(int)ValidPasswordType.SpecialChar] = true;
        }
        if(typeArray.Select(x=>x).ToArray().Length < 3)return false;
        var newUserName = new StringBuilder();
        foreach (var value in userName)
        {
            if(value == point)continue;
            var newChar = char.IsDigit(value) ? value : char.ToLower(value);
            newUserName.Append(newChar);
        }
        return !passWordOrigin.Contains(newUserName.ToString());
    }

    private bool CheckUsername(string oriUserName)
    {
        var username = oriUserName.AsSpan();
        char point = '.';
        if (username.Length is < 6 or > 20)
            return false;
        if (char.IsDigit(username[0]))
        {
            return false;
        }

        if (username[0] == point || username[username.Length - 1] == point) return false;
        var count = false;
        foreach (var value in username)
        {
            if (!char.IsDigit(value) && !char.IsLetter(value)) return false;
            if (value == point && !count)
                count = true;
            else if (value == point && count)
            {
                return false;
            }
            else
            {
                count = false;
            }
        }
        return true;
    }
}
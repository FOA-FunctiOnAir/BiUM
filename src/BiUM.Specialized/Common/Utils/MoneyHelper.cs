using System;

namespace BiUM.Specialized.Common.Utils;

public static class MoneyHelper
{
    private static readonly string[] _ones = ["", "Bir", "İki", "Üç", "Dört", "Beş", "Altı", "Yedi", "Sekiz", "Dokuz"];
    private static readonly string[] _tens = ["", "On", "Yirmi", "Otuz", "Kırk", "Elli", "Altmış", "Yetmiş", "Seksen", "Doksan"];
    private static readonly string[] _hundreds = ["", "Yüz", "İki Yüz", "Üç Yüz", "Dört Yüz", "Beş Yüz", "Altı Yüz", "Yedi Yüz", "Sekiz Yüz", "Dokuz Yüz"];

    public static string ConvertMoneyToWords(decimal amount, string currencyName = "Türk Lirası", string currencyCoinName = "Kuruş")
    {
        if (amount == 0)
        {
            return $"Sıfır {currencyName}";
        }

        var isNegative = amount < 0;
        amount = Math.Abs(amount);

        var intPart = (int)amount;
        var decimalPart = (int)((amount - intPart) * 100);

        var intPartWords = intPart != 0 ? ConvertIntegerToWords(intPart) + $" {currencyName}" : "";
        var decimalPartWords = decimalPart != 0 ? ConvertThreeDigitsToWords(decimalPart) + $" {currencyCoinName}" : "";

        var result = "";

        if (!string.IsNullOrEmpty(intPartWords) && !string.IsNullOrEmpty(decimalPartWords))
        {
            result = $"{intPartWords} {decimalPartWords}";
        }
        else
        {
            result = intPartWords + decimalPartWords;
        }

        return isNegative ? "Eksi " + result : result;
    }

    private static string ConvertIntegerToWords(int number)
    {
        if (number == 0) return "Sıfır";

        var words = "";
        var millions = number / 1000000;
        var thousands = number % 1000000 / 1000;
        var hundreds = number % 1000;

        if (millions > 0)
        {
            words += ConvertThreeDigitsToWords(millions) + " Milyon ";
        }

        if (thousands > 0)
        {
            if (thousands == 1)
            {
                words += "Bin ";
            }
            else
            {
                words += ConvertThreeDigitsToWords(thousands) + " Bin ";
            }
        }

        if (hundreds > 0)
        {
            words += ConvertThreeDigitsToWords(hundreds);
        }

        return words.Trim();
    }

    private static string ConvertThreeDigitsToWords(int number)
    {
        var hundredsPlace = number / 100;
        var tensPlace = number % 100 / 10;
        var onesPlace = number % 10;

        var words = "";

        if (hundredsPlace > 0)
        {
            words += _hundreds[hundredsPlace] + " ";
        }

        if (tensPlace > 0)
        {
            words += _tens[tensPlace] + " ";
        }

        if (onesPlace > 0)
        {
            words += _ones[onesPlace];
        }

        return words.Trim();
    }
}
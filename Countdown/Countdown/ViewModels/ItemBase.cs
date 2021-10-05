﻿using System;

namespace Countdown.ViewModels
{
    internal abstract class ItemBase : IComparable<ItemBase>
    {
        public string Content { get; }

        public ItemBase(string item)
        {
            Content = item;
        }

        public int CompareTo(ItemBase? other)
        {
            // shorter strings first, then reverse alphabetical (numbers before parenthesis)
            int lengthCompare = Content.Length - other!.Content.Length;
            return lengthCompare == 0 ? string.Compare(other.Content, Content, StringComparison.Ordinal) : lengthCompare;
        }

        public override string ToString() => Content;
    }
}
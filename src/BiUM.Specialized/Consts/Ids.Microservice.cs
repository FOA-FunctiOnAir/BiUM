﻿using BiUM.Specialized.Common.Utils;

namespace BiUM.Specialized.Consts;

public partial class Ids
{
    public static class Microservice
    {
        public static class Accounting
        {
            public static Guid Id = GuidGenerator.NewGuid("Microservice-Accounting");
        }

        public static class Accounts
        {
            public static Guid Id = GuidGenerator.NewGuid("Microservice-Accounts");
        }

        public static class Authentication
        {
            public static Guid Id = GuidGenerator.NewGuid("Microservice-Authentication");
        }

        public static class Bpmn
        {
            public static Guid Id = GuidGenerator.NewGuid("Microservice-Bpmn");
        }

        public static class Configuration
        {
            public static Guid Id = GuidGenerator.NewGuid("Microservice-Configuration");
        }

        public static class Customers
        {
            public static Guid Id = GuidGenerator.NewGuid("Microservice-Customers");
        }

        public static class Information
        {
            public static Guid Id = GuidGenerator.NewGuid("Microservice-Information");
        }

        public static class Parameters
        {
            public static Guid Id = GuidGenerator.NewGuid("Microservice-Parameters");
        }

        public static class Payments
        {
            public static Guid Id = GuidGenerator.NewGuid("Microservice-Payments");
        }

        public static class Products
        {
            public static Guid Id = GuidGenerator.NewGuid("Microservice-Products");
        }

        public static class Repair
        {
            public static Guid Id = GuidGenerator.NewGuid("Microservice-Repair");
        }

        public static class Sales
        {
            public static Guid Id = GuidGenerator.NewGuid("Microservice-Sales");
        }

        public static class Stocks
        {
            public static Guid Id = GuidGenerator.NewGuid("Microservice-Stocks");
        }

        public static class Tickets
        {
            public static Guid Id = GuidGenerator.NewGuid("Microservice-Tickets");
        }

        public static class Treasury
        {
            public static Guid Id = GuidGenerator.NewGuid("Microservice-Treasury");
        }

        public static class UiFramework
        {
            public static Guid Id = GuidGenerator.NewGuid("Microservice-UI-Framework");
        }

        public static class UiWeb
        {
            public static Guid Id = GuidGenerator.NewGuid("Microservice-UI-Web");
        }

        public static class UiMobile
        {
            public static Guid Id = GuidGenerator.NewGuid("Microservice-UI-Mobile");
        }
    }
}
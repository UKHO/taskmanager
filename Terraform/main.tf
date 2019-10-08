terraform {
  backend "azurerm" {
    resource_group_name           = "TaskmanagerDev-RG"
    storage_account_name          = "taskmanagerdev"
    container_name                = "tfstate"
    key                           = "projectdev.tfstate"
  }
}

locals {
  nsb-role                         ="nsb"
  api-role                         ="api"
}


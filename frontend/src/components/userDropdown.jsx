import React from "react";
import DropdownComponent from "./DropdownComponent";
import { ChromeTabs, Client, TabEditor } from "../../lib";
import { LoginBL } from "../forms/login";

const UserDropdown = ({ editForm }) => {
  const handleLogout = (event) => {
    deleteMethod();
  };

  const deleteMethod = () => {
    Client.Token = null;
    localStorage.removeItem("UserInfo");
    ChromeTabs.tabs.forEach((x) => x.content.dispose());
    window.location.reload();
  };

  const toggleContent = (
    <>
      <div className="label">
        <span></span>
        <div style={{ whiteSpace: "nowrap" }}>{Client.Token.userName}</div>
      </div>
      <img
        className="img-user"
        src={Client.Token.avatar || "/assets/images/avatar1.png"}
        alt="user"
        srcSet=""
      />
    </>
  );

  const dropdownContent = (
    <>
      <a onClick={handleLogout} className="dropdown-item">
        <i className="fal fa-sign-out mr-1"></i> Logout
      </a>
    </>
  );

  return (
    <DropdownComponent
      toggleContent={toggleContent}
      dropdownContent={dropdownContent}
      className="user-dropdown dropdown-menu-end"
    />
  );
};

export default UserDropdown;

import React from "react";
import DropdownComponent from "./dropdownComponent";
import { ChromeTabs, Client, TabEditor } from "../../lib";
import { LoginBL } from "../forms/login";

const userDropdown = ({ editForm }) => {
  const handleLogout = (event) => {
    deleteMethod();
  };

  const deleteMethod = () => {
    Client.token = null;
    localStorage.removeItem("userInfo");
    ChromeTabs.tabs.forEach((x) => x.content.dispose());
    window.location.reload();
  };

  const toggleContent = (
    <>
      <div className="label">
        <span></span>
        <div style={{ whiteSpace: "nowrap" }}>{Client.token.userName}</div>
      </div>
      <img
        className="img-user"
        src={Client.token.avatar || "/assets/images/avatar1.png"}
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

export default userDropdown;

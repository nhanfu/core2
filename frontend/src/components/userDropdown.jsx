import { ChromeTabs, Client } from "../../lib";

const UserDropdown = ({ editForm: _editForm }) => {
  const handleLogout = (_event) => {
    deleteMethod();
  };

  const deleteMethod = () => {
    Client.Token = null;
    localStorage.removeItem("UserInfo");
    ChromeTabs.tabs.forEach((x) => x.content.Dispose());
    window.location.reload();
  };

  const toggleContent = (
    <>
      <div className="label">
        <span></span>
        <div style={{ whiteSpace: "nowrap" }}>{Client.Token.UserName}</div>
      </div>
      <img
        className="img-user"
        src={Client.Token.Avatar || "/assets/images/avatar1.png"}
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

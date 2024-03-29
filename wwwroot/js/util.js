function addActive(id) {
  document.getElementById(id)?.classList.add("active");
}

function removeActive(id) {
  document.getElementById(id)?.classList.remove("active");
}

function addClass(id, className) {
  document.getElementById(id)?.classList.add(className);
}

function removeClass(id, className) {
  document.getElementById(id)?.classList.remove(className);
}

function removeHighlight(id) {
  document.getElementById(id)?.classList.remove("highlight");
  document.getElementById(id)?.classList.remove("highlight-start");
  document.getElementById(id)?.classList.remove("highlight-end");
}

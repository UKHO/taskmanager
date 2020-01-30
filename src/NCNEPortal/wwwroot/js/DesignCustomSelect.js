
// Used from https://www.w3schools.com/howto/howto_custom_select.asp

//Warning, if called multiple times,
//it will add another designcustomselect where one already exists
function initDesignCustomSelect() {

    var designCustomSelect, i, j, selElmnt, selSelectedElmnt, b, c;
    /* Look for any elements with the class "custom-select": */
    designCustomSelect = document.getElementsByClassName('design-custom-select');
    for (i = 0; i < designCustomSelect.length; i++) {
        selElmnt = designCustomSelect[i].getElementsByTagName('select')[0];
        /* For each element, create a new DIV that will act as the selected item: */
        selSelectedElmnt = document.createElement('DIV');
        selSelectedElmnt.setAttribute('class', 'select-selected');
        selSelectedElmnt.innerHTML = selElmnt.options[selElmnt.selectedIndex].innerHTML;
        if (selSelectedElmnt.innerHTML === "") {
            selSelectedElmnt.innerHTML = "&nbsp;";
        }
        designCustomSelect[i].appendChild(selSelectedElmnt);
        /* For each element, create a new DIV that will contain the option list: */
        b = document.createElement('DIV');
        b.setAttribute('class', 'select-items select-hide');
        for (j = 1; j < selElmnt.length; j++) {
            /* For each option in the original select element,
            create a new DIV that will act as an option item: */
            c = document.createElement('DIV');
            c.innerHTML = selElmnt.options[j].innerHTML;
            c.addEventListener('click', clickOption);
            b.appendChild(c);
        }
        designCustomSelect[i].appendChild(b);
        selSelectedElmnt.addEventListener('click', openSelect);
    }

    /* If the user clicks anywhere outside the select box,
    then close all select boxes: */
    document.addEventListener('click', closeAllSelect);
}

function openSelect(e) {
    /* When the select box is clicked, close any other select boxes,
    and open/close the current select box: */
    console.log('clicked');
    e.stopPropagation();
    closeAllSelect(this);
    this.nextSibling.classList.toggle('select-hide');
    this.classList.toggle('select-arrow-active');
    var iconEls = this.parentNode.getElementsByClassName('arrow-icon');
    for (var iconIndex = 0; iconIndex < iconEls.length; iconIndex++) {
        iconEls[iconIndex].classList.toggle('fa-chevron-up');
        iconEls[iconIndex].classList.toggle('fa-chevron-down');
    }
}

function closeAllSelect(elmnt) {
    /* A function that will close all select boxes in the document,
    except the current select box: */
    var selectItems,
        selected,
        i,
        arrNo = [];
    selectItems = document.getElementsByClassName('select-items');
    selected = document.getElementsByClassName('select-selected');
    for (i = 0; i < selected.length; i++) {
        if (elmnt == selected[i]) {
            arrNo.push(i);
        } else {
            selected[i].classList.remove('select-arrow-active');
        }
    }
    for (i = 0; i < selectItems.length; i++) {
        if (arrNo.indexOf(i)) {
            selectItems[i].classList.add('select-hide');
        }
    }
}

function clickOption (e) {
    /* When an item is clicked, update the original select box,
      and the selected item: */
    var y, i, k, selectElement, h;
    selectElement = this.parentNode.parentNode.getElementsByTagName('select')[0];
    h = this.parentNode.previousSibling;
    for (i = 0; i < selectElement.length; i++) {
        if (selectElement.options[i].innerHTML == this.innerHTML) {
            selectElement.selectedIndex = i;
            h.innerHTML = this.innerHTML;
            y = this.parentNode.getElementsByClassName('same-as-selected');
            for (k = 0; k < y.length; k++) {
                y[k].removeAttribute('class');
            }
            this.setAttribute('class', 'same-as-selected');
            break;
        }
    }
    //Set select attribute on option element
    var optionElements = selectElement.getElementsByTagName("option");

    for (i = 0; i < optionElements.length; i++) {
        optionElements[i].removeAttribute("selected");

        if (this.innerText === optionElements[i].innerText) {
            optionElements[i].setAttribute("selected", "selected");
        }
    }
    //
    h.click();
}
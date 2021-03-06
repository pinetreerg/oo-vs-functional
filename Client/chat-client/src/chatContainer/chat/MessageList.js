import React from 'react'
import { ListGroup, ListGroupItem } from 'react-bootstrap'

const Message = ({order, from, text}) => (
    <ListGroupItem>
        <strong>{from}: </strong>
        <span>{text}</span>
    </ListGroupItem>
)

export default ({messages}) => {
    if(!messages) return null;

    var listItems = messages.map((m) => <Message key={m.order} order={m.order} from={m.from} text={m.text}/>);
    return <ListGroup>{listItems}</ListGroup>
}